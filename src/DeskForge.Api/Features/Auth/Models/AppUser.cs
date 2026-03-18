using DeskForge.Api.Common.Enums;
using DeskForge.Api.Features.Organizations.Models;
using Microsoft.AspNetCore.Identity;

namespace DeskForge.Api.Features.Auth.Models;

public class AppUser : IdentityUser<Guid>
{
    public string FirstName { get; set; } 
    public string LastName { get; set; } 
    public Guid OrganizationId { get; init; }
    public OrgRole Role { get; init; }
    public bool IsOwner => Role == OrgRole.Owner;
    
}