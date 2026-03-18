using System.Security.Claims;
using DeskForge.Api.Common.Entities;
using DeskForge.Api.Features.Auth.Models;
using DeskForge.Api.Features.Organizations.Models;
using DeskForge.Api.Infrastructure.Auth.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace DeskForge.Api.Infrastructure.Persistence;

public class AppDbContext(
    DbContextOptions<AppDbContext> options,
    IHttpContextAccessor httpContextAccessor)
    : IdentityDbContext<AppUser, IdentityRole<Guid>, Guid> (options)
{
    public DbSet<Organization>  Organizations => Set<Organization>();
    public DbSet<RefreshToken> RefreshTokens  => Set<RefreshToken>();
    public DbSet<OrgInvite> Invitations => Set<OrgInvite>();
    

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = new CancellationToken())
    {
        
        var entries = ChangeTracker.Entries<AuditableEntity>()
            .Where(e => e.State == EntityState.Added || e.State == EntityState.Modified);
        
        var user = httpContextAccessor.HttpContext?.User;
        var userIdClaim = user?.FindFirstValue(ClaimTypes.NameIdentifier);
        Guid? currentUserId = Guid.TryParse(userIdClaim, out var id) ? id : null;
        
        foreach (var entry in entries)
        {
            if (entry.State == EntityState.Added)
            {
                entry.Entity.CreatedAtUtc = DateTime.UtcNow;
                entry.Entity.CreatedById = currentUserId;
            }
            
            entry.Entity.LastModifiedAtUtc = DateTime.UtcNow;
            entry.Entity.LastModifiedById = currentUserId;
        }
        return base.SaveChangesAsync(cancellationToken);
    }
}