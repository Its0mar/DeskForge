using System.Security.Claims;
using DeskForge.Api.Common.Enums;
using DeskForge.Api.Infrastructure.Auth.Models;

namespace DeskForge.Api.Infrastructure.Auth;

public static class UserContextMiddleware
{
    public static UserContext Load(ClaimsPrincipal principal)
    {
        if (principal.Identity?.IsAuthenticated is not true)
            return new UserContext{UserId = Guid.Empty, OrganizationId = Guid.Empty, Role = OrgRole.Customer};

        Guid.TryParse(
            principal.FindFirstValue(ClaimTypes.NameIdentifier), out var userId);

        Guid.TryParse(
            principal.FindFirstValue("org_id"), out var orgId);

        Enum.TryParse<OrgRole>(
            principal.FindFirstValue("org_role"), out var role);

        return new UserContext{UserId = userId, OrganizationId = orgId, Role = role};
    }
}