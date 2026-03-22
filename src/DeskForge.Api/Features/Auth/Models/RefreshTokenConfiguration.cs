using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DeskForge.Api.Features.Auth.Models;

public class RefreshTokenConfiguration : IEntityTypeConfiguration<RefreshToken>
{
    public void Configure(EntityTypeBuilder<RefreshToken> builder)
    {
        builder.ToTable("RefreshTokens");

        builder.HasKey(x => x.Id);
        
        builder.Property(x => x.Token)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(x => x.UserId)
            .IsRequired();

        builder.Property(x => x.ExpiresOnUtc)
            .IsRequired()
            .HasConversion(
                v => v.UtcDateTime,           // DateTimeOffset → DateTime (stored in SQLite)
                v => new DateTimeOffset(v, TimeSpan.Zero));  // DateTime → DateTimeOffset (read back)

        builder.Property(x => x.IsRevoked)
            .HasDefaultValue(false);
        
        builder.HasIndex(x => x.Token).IsUnique();
        builder.HasIndex(x => x.UserId);

        builder.HasOne<AppUser>()
            .WithMany()
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}