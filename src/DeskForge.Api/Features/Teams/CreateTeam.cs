using System.Diagnostics.CodeAnalysis;
using DeskForge.Api.Features.Teams.Models;
using DeskForge.Api.Infrastructure.Auth.Models;
using DeskForge.Api.Infrastructure.Persistence;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Wolverine.Http;

namespace DeskForge.Api.Features.Teams;

[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)]
public sealed record CreateTeamCommand(string Name, string? Description);


public sealed class CreateTeamCommandValidator : AbstractValidator<CreateTeamCommand>
{
    public     CreateTeamCommandValidator()
    {
        RuleFor(x => x.Name).Length(3, 30).NotEmpty().WithMessage("Team name length must be between 3 and 30 characters.");
        RuleFor(x => x.Description).MaximumLength(500).WithMessage("Team description length must be less than 500 characters.");
    }
}

[Tags("Teams")]
public static class CreateTeamEndpoint
{
    public static async Task<ProblemDetails> ValidateAsync(
        CreateTeamCommand command,
        AppDbContext db,
        CancellationToken ct)
    {
        var teamExist = await db.Teams.AnyAsync(t => t.Name == command.Name, ct);

        if (teamExist)
        {
            return new ProblemDetails
            {
                Status = StatusCodes.Status409Conflict,
                Title = "Team already exists",
                Detail = "Team already exists"
            };
        }

        return WolverineContinue.NoProblems;
    }

    [Authorize(Policy = "OwnerOrManager")]
    [WolverinePost("api/teams")]
    [EndpointSummary("CreateTeam")]
    public static async Task<Results<Ok<Guid>, ProblemHttpResult>> Handle(CreateTeamCommand command, UserContext currentUser,  AppDbContext db, CancellationToken ct)
    {
        var team = new Team {Name =  command.Name, Description = command.Description, OrganizationId = currentUser.OrganizationId};

        await db.Teams.AddAsync(team, ct);
        await db.SaveChangesAsync(ct);
        
        return TypedResults.Ok(team.Id);
    }
}