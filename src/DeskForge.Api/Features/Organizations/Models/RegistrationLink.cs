using System.Security.Cryptography;
using DeskForge.Api.Common.Entities;

namespace DeskForge.Api.Features.Organizations.Models;

public class RegistrationLink : AuditableEntity
{
    public string Token { get; private set; } =  string.Empty;
    public DateTime? ExpiresAt { get; private init; }
    public int UsageCount { get; private set; }
    public int? MaxUsage { get; private init; }
    
    public bool IsValid =>
        IsActive &&
        (ExpiresAt is null || ExpiresAt > DateTime.UtcNow) && 
        (MaxUsage is null || UsageCount < MaxUsage);
    
    private  RegistrationLink() { }

    public static RegistrationLink Create(DateTime? expiresAt, int? maxUsage)
    {
        return new RegistrationLink
        {
            Token = Convert.ToBase64String(
                    RandomNumberGenerator.GetBytes(12))
                .Replace("/", "-")
                .Replace("+", "_"),
            ExpiresAt = expiresAt,
            MaxUsage = maxUsage
        };
    }
    
    public void IncrementUsage() => UsageCount++;
    public void Deactivate()     => IsActive = false;
    public void Activate()       => IsActive = true;
}