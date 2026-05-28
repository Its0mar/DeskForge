using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DeskForge.Api.Features.Organizations.Models;

public class OrgInviteConfiguration : IEntityTypeConfiguration<OrgInvite>
{
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
        
        // builder.HasOne<AppUser>()
        //     .WithMany()
        //     .HasForeignKey(e => e.CreatedUserId)
        //     .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(o => o.CreatedBy)
            .WithMany()
            .HasForeignKey(o => o.CreatedById)
            .OnDelete(DeleteBehavior.NoAction);
            
    }
}