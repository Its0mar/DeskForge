using System.Security.Claims;
using DeskForge.Api.Common.Enums;

namespace DeskForge.Api.Infrastructure.Auth;

public static class UserContextMiddleware
{
    public static UserContext Load(ClaimsPrincipal principal)
    {
        if (principal.Identity?.IsAuthenticated is not true)
            return new UserContext(Guid.Empty, Guid.Empty, OrgRole.Customer);

        Guid.TryParse(
            principal.FindFirstValue(ClaimTypes.NameIdentifier), out var userId);

        Guid.TryParse(
            principal.FindFirstValue("org_id"), out var orgId);

        Enum.TryParse<OrgRole>(
            principal.FindFirstValue("org_role"), out var role);

        return new UserContext(userId, orgId, role);
    }
}