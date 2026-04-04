using DeskForge.Api.Common.Enums;
using DeskForge.Api.Common.Models;
using DeskForge.Api.Features.Auth.Models;
using DeskForge.Api.Infrastructure.Auth.Token;
using DeskForge.Api.Infrastructure.Persistence;
using FluentValidation;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Wolverine.Http;

namespace DeskForge.Api.Features.Auth.Register;

public sealed record PublicRequesterRegisterCommand(
    string UserName,
    string Email,
    string Password,
    string FirstName,
    string LastName);

public sealed class PublicRequesterRegisterCommandValidator
    : AbstractValidator<PublicRequesterRegisterCommand>
{
    public PublicRequesterRegisterCommandValidator()
    {
        RuleFor(x => x.UserName).NotEmpty().MinimumLength(3);
        RuleFor(x => x.Email).NotEmpty().EmailAddress();
        RuleFor(x => x.Password).NotEmpty().MinimumLength(6);
        RuleFor(x => x.FirstName).NotEmpty();
        RuleFor(x => x.LastName).NotEmpty();
    }
}

[Tags("Auth")]
public static class PublicRequesterRegisterEndpoint
{
    public static async Task<ProblemDetails> ValidateAsync(
        string orgSlug,
        PublicRequesterRegisterCommand command,
        AppDbContext db,
        CancellationToken ct)
    {
        var org = await db.Organizations
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(o => o.TenantCode == orgSlug, ct);

        if (org is null)
            return new ProblemDetails
            {
                Status = StatusCodes.Status404NotFound,
                Title  = "Not Found",
                Detail = "Organization not found."
            };

        if (!org.IsPublicRegistrationOpen)
            return new ProblemDetails
            {
                Status = StatusCodes.Status403Forbidden,
                Title  = "Registration Closed",
                Detail = "This organization is not accepting new registrations."
            };

        var emailExists = await db.Users
            .AnyAsync(u => u.Email == command.Email, ct);

        if (emailExists)
            return new ProblemDetails
            {
                Status = StatusCodes.Status409Conflict,
                Title  = "Conflict",
                Detail = "Email already registered."
            };

        var userNameExists = await db.Users
            .AnyAsync(u => u.UserName == command.UserName, ct);

        if (userNameExists)
            return new ProblemDetails
            {
                Status = StatusCodes.Status409Conflict,
                Title  = "Conflict",
                Detail = "Username already taken."
            };

        return WolverineContinue.NoProblems;
    }

    [WolverinePost("api/{orgSlug}/register")]
    [EndpointSummary("Public Requester Registration")]
    public static async Task<Results<Ok<TokenResponse>, ProblemHttpResult>> Handle(
        string orgSlug,
        PublicRequesterRegisterCommand command,
        AppDbContext db,
        UserManager<AppUser> userManager,
        ITokenProvider tokenProvider,
        CancellationToken ct)
    {
        var org = await db.Organizations
            .IgnoreQueryFilters()
            .FirstAsync(o => o.TenantCode == orgSlug, ct);

        var user = new AppUser
        {
            UserName       = command.UserName,
            Email          = command.Email,
            FirstName      = command.FirstName,
            LastName       = command.LastName,
            OrganizationId = org.Id,
            Role           = OrgRole.Requester
        };

        var result = await userManager.CreateAsync(user, command.Password);
        if (!result.Succeeded)
            return TypedResults.Problem(
                title:      "Registration Failed",
                detail:     result.Errors.First().Description,
                statusCode: StatusCodes.Status400BadRequest);

        var tokens = await tokenProvider.GenerateTokenAsync(user, ct);
        if (tokens.IsError)
            return TypedResults.Problem(
                title:      tokens.TopError.Code,
                detail:     tokens.TopError.Description,
                statusCode: StatusCodes.Status500InternalServerError);

        return TypedResults.Ok(tokens.Value);
    }
}