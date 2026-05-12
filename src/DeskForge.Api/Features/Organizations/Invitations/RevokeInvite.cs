using DeskForge.Api.Common.Enums;
using DeskForge.Api.Infrastructure.Auth.Models;
using DeskForge.Api.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Wolverine.Http;

namespace DeskForge.Api.Features.Organizations.Invitations;


[Tags("Invitations")]
public class RevokeInviteEndpoint
{
    public static async Task<ProblemDetails> ValidateAsync(
        [FromRoute] Guid invitationId,
        UserContext currentUser,
        AppDbContext db,
        CancellationToken ct)
    {
        var invitation = await db.Invitations.FirstOrDefaultAsync(inv => inv.Id == invitationId, ct);
        if (invitation is null)
        {
            return new ProblemDetails
            {
                Status = StatusCodes.Status404NotFound,
                Title = "Invitation not found",
                Detail = "Invitation not found"
            };
        }

        if (!invitation.IsValid)
        {
            return new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Invitation invalid",
                Detail = "Invitation invalid"
            };
        }

        if (currentUser.Role != OrgRole.Owner && invitation.Role == OrgRole.Manager)
        {
            return new ProblemDetails
            {
                Status = StatusCodes.Status403Forbidden,
                Title = "You are not authorized to revoke",
                Detail = "You are not authorized to revoke"
            };
        }

        return WolverineContinue.NoProblems;
    }

    [Authorize(Roles = "Owner, Manager")]
    [WolverineDelete("api/organizations/invites/{invitationId}")]
    public async Task<Results<NoContent, ProblemHttpResult>> Handle(
        [FromRoute] Guid invitationId,
        [NotBody] AppDbContext db,
        [NotBody] CancellationToken ct)
    {
        var invite = await db.Invitations.FirstOrDefaultAsync(inv => inv.Id == invitationId, ct);
        invite!.Revoke();
        await db.SaveChangesAsync(ct);

        return TypedResults.NoContent();
    }
}