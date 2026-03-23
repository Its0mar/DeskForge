using System.Diagnostics.CodeAnalysis;
using DeskForge.Api.Features.Teams.Models;
using DeskForge.Api.Infrastructure.Auth.Models;
using DeskForge.Api.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Wolverine.Http;

namespace DeskForge.Api.Features.Teams;

[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)]
public sealed record AddTeamMemberCommand(Guid UserId, Guid TeamId);

[Tags("Team")]
public static class AddTeamMember
{
    public static async Task<ProblemDetails> ValidateAsync(
        AddTeamMemberCommand command,
        AppDbContext db,
        CancellationToken ct)
    {
        var user = await db.Users.FirstOrDefaultAsync(u => u.Id == command.UserId, ct);

        if (user is null)
        {
            return new ProblemDetails
            {
                Status = StatusCodes.Status404NotFound,
                Title = "User not found",
                Detail = "User not found"
            };
        }

        var teamExist = await db.Teams.AnyAsync(t => t.Id == command.TeamId, ct);
        if (!teamExist)
        {
            return new ProblemDetails
            {
                Status = StatusCodes.Status404NotFound,
                Title = "Team not found",
                Detail = "Team not found"
            };
        }
        
        var userExistInTeam = await db.TeamMemberships.AnyAsync(t => t.UserId == command.UserId
                && t.UserId == command.UserId, ct);
        if (userExistInTeam)
        {
            return new ProblemDetails
            {
                Status = StatusCodes.Status409Conflict,
                Title = "User already exists",
                Detail = "User already exists"
            };
        }
        
        return WolverineContinue.NoProblems;
    }
    
    [Authorize(Policy = "OwnerOrManager")]
    [WolverinePost("api/teams/members")]
    [EndpointSummary("AddTeamMember")]
    public static async Task<Results<Ok<Guid>, ProblemHttpResult>> Handle(
        AddTeamMemberCommand command,
        UserContext currentUser,
        AppDbContext db,
        CancellationToken ct)
    {
        var teamMember = TeamMembership.Create(command.TeamId, command.UserId, currentUser.UserId);

        db.TeamMemberships.Add(teamMember);
        await db.SaveChangesAsync(ct);

        return TypedResults.Ok(teamMember.Id);
    }
    
}