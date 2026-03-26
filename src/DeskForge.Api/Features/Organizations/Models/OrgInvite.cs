using DeskForge.Api.Common.Entities;
using DeskForge.Api.Common.Enums;

namespace DeskForge.Api.Features.Organizations.Models;

public class OrgInvite : AuditableEntity
{
    public string Email { get; private set; } = string.Empty;
    public string InviteToken { get; private set; } = string.Empty; 
    public DateTimeOffset ExpiresAtUtc { get; private init;}
    public OrgRole Role { get; private set; }
    public Guid? CreatedUserId { get; private set; } 
    public bool IsRevoked { get; private set; } =  false;
    public bool IsAccepted => CreatedUserId.HasValue;
    public bool IsValid => !IsRevoked && !IsAccepted && DateTimeOffset.UtcNow < ExpiresAtUtc;
    
    private OrgInvite()
    { }
    
    public static OrgInvite Create(string email, OrgRole role, Guid orgId, int expiryDays = 7)
    {
        return new OrgInvite
        {
            Email = email.ToLower().Trim(),
            Role = role,
            OrganizationId = orgId,
            InviteToken = Guid.NewGuid().ToString("N"),
            ExpiresAtUtc = DateTimeOffset.UtcNow.AddDays(expiryDays)
        };
    }
    
    public void Accept(Guid createdUserId)
    {
        if (IsValid)
        {
            CreatedUserId = createdUserId;
        }
    }
    
    public void Revoke() => IsRevoked = true;
}