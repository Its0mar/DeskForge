using DeskForge.Api.Common.Enums;
using DeskForge.Api.Infrastructure.Auth.Models;
using DeskForge.Api.Infrastructure.Persistence;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Wolverine.Http;

namespace DeskForge.Api.Features.Organizations.Settings;

public record DeleteOrganizationCommand(string ConfirmationName);

public sealed class DeleteOrganizationCommandValidator : AbstractValidator<DeleteOrganizationCommand>
{
    public DeleteOrganizationCommandValidator()
    {
        RuleFor(x => x.ConfirmationName)
            .NotEmpty().WithMessage("You must type the organization name to confirm deletion.");
    }
}


[Tags("Organizations - Settings")]
public static class DeleteOrganizationEndpoint
{
    public static async Task<ProblemDetails> ValidateAsync(
        DeleteOrganizationCommand command,
        UserContext currentUser,
        AppDbContext db,
        CancellationToken ct)
    {
        if (currentUser.Role != OrgRole.Owner)
        {
            return new ProblemDetails
            {
                Status = StatusCodes.Status403Forbidden,
                Title = "Not authorized",
                Detail = "Only the Organization Owner can close the account."
            };
        }

        var organization = await db.Organizations
            .FirstOrDefaultAsync(o => o.Id == currentUser.OrganizationId, ct);

        if (organization is null)
        {
            return new ProblemDetails { Status = StatusCodes.Status404NotFound };
        }

        if (!string.Equals(organization.Name, command.ConfirmationName, StringComparison.OrdinalIgnoreCase))
        {
            return new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Confirmation failed",
                Detail = "The provided name does not match the organization name."
            };
        }

        return WolverineContinue.NoProblems;
    }

    [Authorize(Roles = "Owner")]
    [WolverineDelete("api/organizations")]
    public static async Task<Results<NoContent, NotFound>> Handle(
        DeleteOrganizationCommand command,
        UserContext currentUser,
        AppDbContext db,
        CancellationToken ct)
    {
        var user = await db.Users.Include(u => u.Organization)
            .FirstAsync(o => o.OrganizationId == currentUser.OrganizationId,
                ct);

        if (user.Organization is null)
            return TypedResults.NotFound();
        
        user.Delete();
        user.Organization.CloseAccount();
        
        await db.SaveChangesAsync(ct);

        return TypedResults.NoContent();
    }
}