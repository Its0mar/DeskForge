using System.Diagnostics.CodeAnalysis;
using DeskForge.Api.Common.Results;
using DeskForge.Api.Features.Organizations.Models;
using DeskForge.Api.Infrastructure.Persistence;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Wolverine.Http;

namespace DeskForge.Api.Features.Organizations;

[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)]
public sealed record CreateOrganizationCommand(string Name, string TenantCode);

public sealed class CreateOrganizationCommandValidator : AbstractValidator<CreateOrganizationCommand>
{
    public CreateOrganizationCommandValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MinimumLength(3);
        RuleFor(x => x.TenantCode)
            .NotEmpty().Matches("^[a-z0-9-]+$")
            .Must(x => !x.StartsWith("-") && !x.EndsWith("-"))
            .WithMessage("Tenant code cannot start or end with a hyphen.");;
    }
}

public static class CreateOrganizationHandler
{
    public static async Task<ProblemDetails> ValidateAsync(CreateOrganizationCommand command, AppDbContext context, CancellationToken ct)
    {
        var isTenantExist = await context.Organizations.AnyAsync(org => org.TenantCode == command.TenantCode, ct);
        if (isTenantExist)
        {
            return new ProblemDetails
            {
                Status = StatusCodes.Status409Conflict,
                Title = "Conflict",
                Detail = "This tenant code is already taken."
            };
        }

        return WolverineContinue.NoProblems;
    }

    public static async Task<Result<Guid>> Handle(CreateOrganizationCommand command, AppDbContext context, CancellationToken ct)
    {
        var organization = new Organization
        {
            Name = command.Name,
            TenantCode = command.TenantCode.ToLower().Trim()
        };

        context.Organizations.Add(organization);
        await context.SaveChangesAsync(ct);

        return organization.Id;
    }
}