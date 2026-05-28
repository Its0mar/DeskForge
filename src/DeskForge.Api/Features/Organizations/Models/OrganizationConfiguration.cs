using DeskForge.Api.Features.Auth.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DeskForge.Api.Features.Organizations.Models;

public class OrganizationConfiguration : IEntityTypeConfiguration<Organization>
{
    public void Configure(EntityTypeBuilder<Organization> builder)
    {
        builder.HasKey(o => o.Id);
        
        builder.Ignore(x => x.OrganizationId);
        
        builder.HasIndex(e => e.TenantCode).IsUnique();
        builder.Property(e => e.TenantCode).HasMaxLength(50).IsRequired();
        
        builder.Property(e => e.Name).HasMaxLength(100).IsRequired();
        
        // AuditableEntity contributes CreatedById / LastModifiedById which EF would
        // auto-wire as shadow FK relationships to AspNetUsers with Cascade delete.
        // Combined with the AppUser→Organization FK, this creates a circular cascade
        // path that SQL Server rejects. Map them as plain scalar columns instead.
        builder.Property(e => e.CreatedById).HasColumnName("CreatedById");
        builder.Property(e => e.LastModifiedById).HasColumnName("LastModifiedById");
    }
    
}