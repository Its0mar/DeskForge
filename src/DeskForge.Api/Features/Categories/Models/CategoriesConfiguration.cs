using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DeskForge.Api.Features.Categories.Models;

public class CategoryConfiguration : IEntityTypeConfiguration<Category>
{
    public void Configure(EntityTypeBuilder<Category> builder)
    {
        builder.HasIndex(e => new {e.Name, e.OrganizationId}).IsUnique();
        
        builder.HasOne(e => e.TargetTeam)
            .WithMany()
            .HasForeignKey(e => e.TargetTeamId)
            .OnDelete(DeleteBehavior.Restrict);
        
        builder.Property(e => e.Name).IsRequired().HasMaxLength(30);
        builder.Property(e => e.Description).HasMaxLength(300);
    }
}