using DeskForge.Api.Common.Dtos;
using DeskForge.Api.Features.Auth.Models;
using DeskForge.Api.Infrastructure.Auth.Token;
using DeskForge.Api.Infrastructure.Persistence;
using FluentValidation;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Wolverine.Http;

namespace DeskForge.Api.Features.Auth.Login;

public sealed record LoginCommand(string Email, string Password);

public sealed class LoginCommandValidator : AbstractValidator<LoginCommand>
{
    public LoginCommandValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress().WithMessage("A valid email address is required.");
        RuleFor(x => x.Password).NotEmpty().MinimumLength(6);
    }
}

[Tags("Auth")]
public static class LoginEndpoint
{
    [WolverinePost("api/auth/login")]
    [EndpointSummary("Login")]
    public static async Task<Results<Ok<TokenResponse>, ProblemHttpResult>> Handle(
        LoginCommand command,
        UserManager<AppUser> userManager,
        ITokenProvider tokenProvider,
        AppDbContext db,
        CancellationToken ct)
    {
        var user = await db.Users
            .AsNoTracking() 
            .FirstOrDefaultAsync(u => u.Email == command.Email, ct);
        
        // var user = await userManager.FindByEmailAsync(command.Email);

        if (user is null)
        {
            return LoginErrors.InvalidCredentials();
        }

        var isPasswordValid = await userManager.CheckPasswordAsync(user, command.Password);

        if (!isPasswordValid)
        {
            return LoginErrors.InvalidCredentials();
        }

        var tokenResult = await tokenProvider.GenerateTokenAsync(user, ct);

        if (tokenResult.IsError)
        {
            return LoginErrors.TokenGenerationError(tokenResult.TopError.Description);
        }

        return TypedResults.Ok(tokenResult.Value);
    }
}

public static class LoginErrors
{
    public static ProblemHttpResult InvalidCredentials() =>
        TypedResults.Problem(
            title: "Unauthorized",
            detail: "Invalid email or password.",
            statusCode: StatusCodes.Status401Unauthorized);

    public static ProblemHttpResult TokenGenerationError(string detail) =>
        TypedResults.Problem(
            title: "Token Error",
            detail: detail,
            statusCode: StatusCodes.Status500InternalServerError);
}