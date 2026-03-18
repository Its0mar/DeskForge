using DeskForge.Api.Common.Enums;

namespace DeskForge.Api.Infrastructure.Auth.Models;



public class UserContext
{
    public Guid UserId { get; set; }
    public Guid OrganizationId { get; set; }
    public OrgRole Role { get; set; }
}