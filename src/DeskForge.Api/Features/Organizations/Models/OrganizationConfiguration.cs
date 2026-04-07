using DeskForge.Api.Features.Auth.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DeskForge.Api.Features.Organizations.Models;

public class OrganizationConfiguration : IEntityTypeConfiguration<Organization>, IEntityTypeConfiguration<OrgInvite>
{
    public void Configure(EntityTypeBuilder<Organization> builder)
    {
        builder.HasKey(o => o.Id);
        
        builder.Ignore(x => x.OrganizationId);
        
        builder.HasIndex(e => e.TenantCode).IsUnique();
        builder.Property(e => e.TenantCode).HasMaxLength(50).IsRequired();
        
        builder.Property(e => e.Name).HasMaxLength(100).IsRequired();
    }

    public void Configure(EntityTypeBuilder<OrgInvite> builder)
    {
        builder.HasKey(e => e.Id);
       
        builder.HasIndex(e => new {e.InviteToken, e.OrganizationId}).IsUnique();
        
        builder.Property(e => e.InviteToken).IsRequired().HasMaxLength(32);
        builder.Property(e => e.Email).IsRequired().HasMaxLength(256);
        
        builder.HasOne<Organization>()
            .WithMany()
            .HasForeignKey(e => e.OrganizationId)
            .OnDelete(DeleteBehavior.Restrict);
        
        builder.HasOne<AppUser>()
            .WithOne()
            .HasForeignKey<OrgInvite>(e => e.CreatedUserId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}