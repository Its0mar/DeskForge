using DeskForge.Api.Common.Enums;
using DeskForge.Api.Infrastructure.Auth.Models;
using DeskForge.Api.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Wolverine.Http;

namespace DeskForge.Api.Features.Organizations;

[Tags("Organizations")]
public class RemoveOrganizationMember
{
    public async Task<ProblemDetails> ValidateAsync(
        [FromRoute] Guid userId,
        [NotBody] UserContext currentUser,
        [NotBody] AppDbContext db,
        [NotBody] CancellationToken ct)
    {
        
        if (currentUser.UserId == userId)
        {
            return new ProblemDetails
            {
                Title = "You can`t delete yourself",
                Status = StatusCodes.Status403Forbidden,
                Detail = "You can`t delete yourself"
            };
        }
        
        var targetUser =  await db.Users.Select(u => new {u.Id, u.Role}).SingleOrDefaultAsync(x => x.Id == userId, ct);
        
        if (targetUser is null)
        {
            return new ProblemDetails
            {
                Title = "User not found",
                Status = StatusCodes.Status404NotFound,
                Detail = "User not found"
            };
        }
        
        // check if user is authorized to do the action
        if (currentUser.Role != OrgRole.Owner && targetUser.Role == OrgRole.Manager)
        {
            return new ProblemDetails
            {
                Title = "You are not authorized to do this action",
                Status = StatusCodes.Status403Forbidden,
                Detail = "You are not authorized to do this action"
            };
        }

        return WolverineContinue.NoProblems;
    }

    [Authorize(Policy = "OwnerOrManager")]
    [WolverineDelete("api/organizations/members/{userId}")]
    public async Task<NoContent> Handle(
        [FromRoute]Guid userId,
        [NotBody] AppDbContext db,
        [NotBody] CancellationToken ct)
    {
        var user =  await db.Users.SingleOrDefaultAsync(x => x.Id == userId, ct);
        user!.Delete();
        db.Users.Update(user);
        await db.SaveChangesAsync(ct);
        
        return TypedResults.NoContent();
    }
}