using System.Linq.Expressions;
using System.Security.Claims;
using DeskForge.Api.Common.Abstractions;
using DeskForge.Api.Common.Entities;
using DeskForge.Api.Features.Auth.Models;
using DeskForge.Api.Features.Categories.Models;
using DeskForge.Api.Features.Organizations.Models;
using DeskForge.Api.Features.Sla.Models;
using DeskForge.Api.Features.Teams.Models;
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
    public DbSet<Team> Teams  => Set<Team>();
    public DbSet<TeamMembership> TeamMemberships => Set<TeamMembership>();
    public DbSet<Category>  Categories => Set<Category>();
    public DbSet<SlaPolicy> SlaPolicies => Set<SlaPolicy>();
    
    
    public Guid CurrentOrgId => GetGuidClaim("org_id");
    
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
        
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            // Check if the entity implements our auditing interface
            if (typeof(IAuditableEntity).IsAssignableFrom(entityType.ClrType))
            {
                var parameter = Expression.Parameter(entityType.ClrType, "e");

                // --- FILTER 1: IsActive == true ---
                var isActiveCheck = Expression.Equal(
                    Expression.Property(parameter, nameof(IAuditableEntity.IsActive)), 
                    Expression.Constant(true));

                // --- FILTER 2: IsDeleted == false ---
                var isNotDeleted = Expression.Equal(
                    Expression.Property(parameter, nameof(IAuditableEntity.IsDeleted)), 
                    Expression.Constant(false));

                // Combine Soft Delete and Active checks
                var baseFilter = Expression.AndAlso(isActiveCheck, isNotDeleted);

                // --- FILTER 3: OrganizationId Check (Conditional) ---
                if (entityType.ClrType == typeof(Organization))
                {
                    // (Orgs don't have an OrgId FK)
                    var orgFilter = Expression.Lambda(baseFilter, parameter);
                    modelBuilder.Entity(entityType.ClrType).HasQueryFilter(orgFilter);
                }
                else
                {
                    // Child entities (Teams, Users, etc.) must match the current User's OrgId
                    var orgMatch = Expression.Equal(
                        Expression.Property(parameter, nameof(IAuditableEntity.OrganizationId)),
                        Expression.Property(Expression.Constant(this), nameof(CurrentOrgId)));

                    var fullFilter = Expression.AndAlso(baseFilter, orgMatch);
                    var finalLambda = Expression.Lambda(fullFilter, parameter);
                
                    modelBuilder.Entity(entityType.ClrType).HasQueryFilter(finalLambda);
                }
            }
        }
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = new CancellationToken())
    {
        
        var user = httpContextAccessor.HttpContext?.User;
        var userIdClaim = user?.FindFirstValue(ClaimTypes.NameIdentifier);
        Guid? currentUserId = Guid.TryParse(userIdClaim, out var userId) ? userId : null;
        
        var orgId = CurrentOrgId;
        
        foreach (var entry in ChangeTracker.Entries<IAuditableEntity>())
        {
            switch (entry.State)
            {
                case EntityState.Added:
                    if (entry.Entity is not Organization && entry.Entity is not AppUser)
                    {
                        entry.Entity.OrganizationId = orgId;
                        
                    }
                    
                    entry.Entity.CreatedAtUtc = DateTime.UtcNow;
                    entry.Entity.CreatedById = currentUserId;
                    entry.Entity.IsActive = true;
                    entry.Entity.IsDeleted = false;
                    
                    entry.Entity.LastModifiedAtUtc = DateTime.UtcNow;
                    entry.Entity.LastModifiedById = currentUserId;
                    break;

                case EntityState.Modified:
                    entry.Entity.LastModifiedAtUtc = DateTime.UtcNow;
                    entry.Entity.LastModifiedById = currentUserId;
                    break;

                case EntityState.Deleted:
                    entry.State = EntityState.Modified;
                    entry.Entity.IsDeleted = true;
                    entry.Entity.LastModifiedAtUtc = DateTime.UtcNow;
                    entry.Entity.LastModifiedById = currentUserId;
                    break;
            }
        }
        return base.SaveChangesAsync(cancellationToken);
    }
    
    
    private Guid GetGuidClaim(string claimType)
    {
        var claim = httpContextAccessor.HttpContext?.User?.FindFirstValue(claimType);
        return Guid.TryParse(claim, out var id) ? id : Guid.Empty;
    }
}