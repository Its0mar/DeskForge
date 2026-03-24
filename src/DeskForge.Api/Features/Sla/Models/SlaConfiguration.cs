using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DeskForge.Api.Features.Sla.Models;

public class SlaPolicyConfiguration : IEntityTypeConfiguration<SlaPolicy>
{
    public void Configure(EntityTypeBuilder<SlaPolicy> builder)
    {
        builder.HasIndex(e => new { e.Priority, e.OrganizationId }).IsUnique();

        builder.Property(e => e.Priority).HasConversion<string>();
    }
}