using DeskForge.Api.Common.Abstractions;
using DeskForge.Api.Common.Enums;
using DeskForge.Api.Features.Organizations.Models;
using Microsoft.AspNetCore.Identity;

namespace DeskForge.Api.Features.Auth.Models;

public class AppUser : IdentityUser<Guid>, IAuditableEntity
{
    public string FirstName { get; set; } 
    public string LastName { get; set; } 

    public OrgRole Role { get; init; }
    public bool IsOwner => Role == OrgRole.Owner;
    public Guid OrganizationId { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }
    public Guid? CreatedById { get; set; }
    public DateTimeOffset LastModifiedAtUtc { get; set; }
    public Guid? LastModifiedById { get; set; }
    public DateTimeOffset? LastAssignedDate { get; set; }

    public bool IsDeleted { get; set; } = false;
    public bool IsActive { get; set; } = true;

    public Organization Organization { get; set; } = null!;
}