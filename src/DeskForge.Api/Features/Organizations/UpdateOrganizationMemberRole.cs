using DeskForge.Api.Common.Enums;
using DeskForge.Api.Infrastructure.Auth.Models;
using DeskForge.Api.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Wolverine.Http;

namespace DeskForge.Api.Features.Organizations;

public sealed record UpdateRoleRequest(OrgRole Role);

[Tags("Organizations")]
public class UpdateOrganizationMemberRole
{
    public async Task<ProblemDetails> ValidateAsync(
        [FromRoute] Guid userId,
        UpdateRoleRequest request,
        [NotBody] UserContext currentUser,
        [NotBody] AppDbContext db,
        [NotBody] CancellationToken ct)
    {
        if (currentUser.UserId == userId)
        {
            return new ProblemDetails { Title = "Forbidden", Status = 403, Detail = "You cannot change your own role." };
        }

        var targetUser = await db.Users
            .Select(u => new { u.Id, u.Role })
            .SingleOrDefaultAsync(x => x.Id == userId, ct);

        if (targetUser is null)
        {
            return new ProblemDetails { Title = "Not Found", Status = 404, Detail = "User not found." };
        }

        if (targetUser.Role == OrgRole.Requester)
        {
            return new ProblemDetails { Title = "Forbidden", Status = 403, Detail = "You cannot modify a Requester's role." };
        }

        if (request.Role == OrgRole.Requester)
        {
            return new ProblemDetails { Title = "Forbidden", Status = 403, Detail = "Internal users cannot be converted to Requesters." };
        }

        if (currentUser.Role == OrgRole.Manager)
        {
            if (targetUser.Role == OrgRole.Owner || targetUser.Role == OrgRole.Manager)
            {
                return new ProblemDetails { Title = "Forbidden", Status = 403, Detail = "You do not have permission to modify this user's role." };
            }

            if (request.Role == OrgRole.Owner || request.Role == OrgRole.Manager)
            {
                return new ProblemDetails { Title = "Forbidden", Status = 403, Detail = "You do not have permission to grant this role." };
            }
        }
        
        if (request.Role == OrgRole.Owner)
        {
            return new ProblemDetails { Title = "Forbidden", Status = 403, Detail = "You do not have permission to grant this role." };
        }

        return WolverineContinue.NoProblems;
    }

    [Authorize(Policy = "OwnerOrManager")]
    [WolverinePut("api/organizations/members/{userId}/role")]
    public async Task<NoContent> Handle(
        [FromRoute] Guid userId,
        UpdateRoleRequest request,
        [NotBody] AppDbContext db,
        [NotBody] CancellationToken ct)
    {
        var user = await db.Users.SingleOrDefaultAsync(x => x.Id == userId, ct);

        user!.UpdateRole(request.Role);
        await db.SaveChangesAsync(ct);
        
        return TypedResults.NoContent();
    }
}