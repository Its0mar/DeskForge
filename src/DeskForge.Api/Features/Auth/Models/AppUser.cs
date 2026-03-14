using DeskForge.Api.Common.Enums;
using Microsoft.AspNetCore.Identity;

namespace DeskForge.Api.Features.Auth.Models;

public class AppUser : IdentityUser
{
    public Guid OrganizationId { get; set; }
    public OrgRole Role { get; set; }
}