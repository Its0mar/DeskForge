using DeskForge.Api.Common.Enums;
using DeskForge.Api.Features.Sla.Models;
using DeskForge.Api.Infrastructure.Persistence;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Wolverine.Http;

namespace DeskForge.Api.Features.Sla;

public sealed record CreateSlaPolicyCommand(TicketPriority Priority,  int ResponseMinutes, int ResolutionMinutes);

public sealed class CreateSlaPolicyCommandValidator : AbstractValidator<CreateSlaPolicyCommand>
{
    public CreateSlaPolicyCommandValidator()
    {
        RuleFor(x => x.Priority)
            .IsInEnum().WithMessage("Invalid priority value.");

        RuleFor(x => x.ResponseMinutes)
            .GreaterThan(0).WithMessage("Response time must be at least 1 minute.")
            .LessThanOrEqualTo(10080).WithMessage("Response time cannot exceed 7 days."); // 7*24*60

        RuleFor(x => x.ResolutionMinutes)
            .GreaterThan(0).WithMessage("Resolution time must be at least 1 minute.")
            .LessThanOrEqualTo(43200).WithMessage("Resolution time cannot exceed 30 days."); // 30*24*60

        RuleFor(x => x.ResolutionMinutes)
            .GreaterThan(x => x.ResponseMinutes)
            .WithMessage("Resolution time must be greater than response time.");

    }
}

[Tags("Sla")]
public static class CreateSlaPolicyEndpoint
{
    public static async Task<ProblemDetails> ValidateAsync(
        CreateSlaPolicyCommand command,
        AppDbContext db,
        CancellationToken ct)
    {
        var slaExist = await db.SlaPolicies.AnyAsync(sla => sla.Priority == command.Priority, ct);
        if (slaExist)
        {
            return new ProblemDetails
            {
                Status = StatusCodes.Status409Conflict,
                Title = "Conflict",
                Detail = $"An SLA policy for {command.Priority} priority already exists."
            };
        }
        return WolverineContinue.NoProblems;
    }

    [Authorize(Policy = "OwnerOrManager")]
    [WolverinePost("api/sla")]
    [EndpointSummary("CreateSlaPolicy")]
    public static async Task<Results<Ok<Guid>, ProblemHttpResult>> Handle(
        CreateSlaPolicyCommand command,
        AppDbContext db,
        CancellationToken ct)
    {
        var policy = SlaPolicy.Create(command.Priority,
            command.ResponseMinutes,
            command.ResolutionMinutes);

        db.SlaPolicies.Add(policy);
        await db.SaveChangesAsync(ct);

        return TypedResults.Ok(policy.Id);
    }
    
}
