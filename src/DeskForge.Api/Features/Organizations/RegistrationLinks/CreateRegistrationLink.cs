using System.Diagnostics.CodeAnalysis;
using DeskForge.Api.Features.Organizations.Models;
using DeskForge.Api.Infrastructure.Auth.Models;
using DeskForge.Api.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Wolverine.Http;

namespace DeskForge.Api.Features.Organizations.RegistrationLinks;

[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)]
public sealed record CreateRegistrationLinkCommand(
    DateTime? ExpiresAt  = null,
    int? MaxUsages       = null);

[Tags("Organizations")]
public static class CreateRegistrationLinkEndpoint
{
    public static ProblemDetails Validate(
        CreateRegistrationLinkCommand command)
    {
        if (command.ExpiresAt.HasValue &&
            command.ExpiresAt <= DateTime.UtcNow)
            return new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title  = "Invalid Expiry",
                Detail = "Expiry date must be in the future."
            };

        if (command.MaxUsages.HasValue && command.MaxUsages <= 0)
            return new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title  = "Invalid Max Usages",
                Detail = "Max usages must be greater than 0."
            };

        return WolverineContinue.NoProblems;
    }
    
    
    [Authorize(Policy = "OwnerOrManager")]
    [WolverinePost("api/organizations/registration-links")]
    [EndpointSummary("Create Registration Link")]
    public static async Task<Ok<Guid>> Handle(
        CreateRegistrationLinkCommand command,
        UserContext currentUser,
        AppDbContext db,
        CancellationToken ct)
    {
        var link = RegistrationLink.Create(
            command.ExpiresAt,
            command.MaxUsages);

        db.RegistrationLinks.Add(link);
        await db.SaveChangesAsync(ct);

        return TypedResults.Ok(link.Id);
    }
}