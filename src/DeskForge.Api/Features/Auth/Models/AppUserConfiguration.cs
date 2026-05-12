using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DeskForge.Api.Features.Auth.Models;

public class AppUserConfiguration : IEntityTypeConfiguration<AppUser>
{
    public void Configure(EntityTypeBuilder<AppUser> builder)
    {
        builder.HasOne(u => u.Organization)
            .WithMany()
            .HasForeignKey(u => u.OrganizationId)
            .OnDelete(DeleteBehavior.Restrict);

        // AppUser implements IAuditableEntity so it has CreatedById / LastModifiedById
        // as plain scalar audit fields — NOT foreign keys to another AppUser.
        // Without this, EF Core sees AuditableEntity.CreatedBy (AppUser navigation) +
        // AppUser.CreatedById and generates a shadow column 'CreatedById1'.
        builder.Property(u => u.CreatedById).HasColumnName("CreatedById");
        builder.Property(u => u.LastModifiedById).HasColumnName("LastModifiedById");
    }
}