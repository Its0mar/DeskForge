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

public sealed record RequesterRegisterCommand(
    string Token,
    string UserName,
    string Email,
    string Password,
    string FirstName,
    string LastName);

public sealed class RequesterRegisterCommandValidator
    : AbstractValidator<RequesterRegisterCommand>
{
    public RequesterRegisterCommandValidator()
    {
        RuleFor(x => x.Token)
            .NotEmpty().WithMessage("Registration token is required.");

        RuleFor(x => x.UserName)
            .NotEmpty().MinimumLength(3);

        RuleFor(x => x.Email)
            .NotEmpty().EmailAddress();

        RuleFor(x => x.Password)
            .NotEmpty().MinimumLength(6);

        RuleFor(x => x.FirstName).NotEmpty();
        RuleFor(x => x.LastName).NotEmpty();
    }
}

[Tags("Auth")]
public static class RequesterRegisterEndpoint
{
    public static async Task<ProblemDetails> ValidateAsync(
        RequesterRegisterCommand command,
        AppDbContext db,
        CancellationToken ct)
    {
        var link = await db.RegistrationLinks
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(l => l.Token == command.Token, ct);

        if (link is null || !link.IsValid)
            return new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title  = "Invalid Token",
                Detail = "Registration link is invalid or expired."
            };

        var emailExists = await db.Users
            .IgnoreQueryFilters()
            .AnyAsync(u => u.Email == command.Email, ct);

        if (emailExists)
            return new ProblemDetails
            {
                Status = StatusCodes.Status409Conflict,
                Title  = "Conflict",
                Detail = "Email already registered."
            };

        var userNameExists = await db.Users
            .IgnoreQueryFilters()
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

    [WolverinePost("api/auth/register/requester")]
    [EndpointSummary("RequesterRegisterViaLink")]
    public static async Task<Results<Ok<TokenResponse>, ProblemHttpResult>> Handle(
        RequesterRegisterCommand command,
        AppDbContext db,
        UserManager<AppUser> userManager,
        ITokenProvider tokenProvider,
        CancellationToken ct)
    {
        var link = await db.RegistrationLinks
            .IgnoreQueryFilters()
            .FirstAsync(l => l.Token == command.Token, ct);

        var user = new AppUser
        {
            UserName       = command.UserName,
            Email          = command.Email,
            FirstName      = command.FirstName,
            LastName       = command.LastName,
            OrganizationId = link.OrganizationId,
            Role           = OrgRole.Requester
        };

        var result = await userManager.CreateAsync(user, command.Password);
        if (!result.Succeeded)
            return TypedResults.Problem(
                title:      "Registration Failed",
                detail:     result.Errors.First().Description,
                statusCode: StatusCodes.Status400BadRequest);

        link.IncrementUsage();
        await db.SaveChangesAsync(ct);

        var tokens = await tokenProvider.GenerateTokenAsync(user, ct);
        if (tokens.IsError)
            return TypedResults.Problem(
                title:      tokens.TopError.Code,
                detail:     tokens.TopError.Description,
                statusCode: StatusCodes.Status500InternalServerError);

        return TypedResults.Ok(tokens.Value);
    }
}