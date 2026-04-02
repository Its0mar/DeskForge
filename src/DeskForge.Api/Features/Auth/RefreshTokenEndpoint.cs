using System.Security.Claims;
using DeskForge.Api.Common.Models;
using DeskForge.Api.Features.Auth.Models;
using DeskForge.Api.Infrastructure.Auth.Token;
using DeskForge.Api.Infrastructure.Persistence;
using FluentValidation;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Wolverine.Http;

namespace DeskForge.Api.Features.Auth;

public sealed record RefreshTokenCommand(string AccessToken, string RefreshToken);

public sealed class RefreshTokenCommandValidator : AbstractValidator<RefreshTokenCommand>
{
    public  RefreshTokenCommandValidator()
    {
        RuleFor(x => x.AccessToken).NotEmpty().WithMessage("Access token is required");
        RuleFor(x => x.RefreshToken).NotEmpty().WithMessage("Refresh token is required");
    }
}

[Tags("Auth")]
public static class RefreshTokenEndpoint
{
    [WolverinePost("api/auth/refresh")]
    [EndpointSummary("Refresh access token")]
    public static async Task<Results<Ok<TokenResponse>, ProblemHttpResult>> Handle(
        RefreshTokenCommand command,
        AppDbContext db,
        UserManager<AppUser> userManager,
        ITokenProvider tokenProvider,
        ILogger<RefreshTokenCommand> logger,
        CancellationToken ct)
    {
        var principal = tokenProvider.GetPrincipalFromExpiredToken(command.AccessToken);
        if (principal is null)
        {
            logger.LogWarning("Token refresh failed: access token could not be validated");
            return TypedResults.Problem(
                title: "Invalid Token",
                detail: "Access token is invalid.",
                statusCode: StatusCodes.Status401Unauthorized);
        }
        
        var userIdClaim = principal.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(userIdClaim, out var userId))
        {
            logger.LogWarning("Token refresh failed: could not parse UserId from claims");
            return TypedResults.Problem(
                title: "Invalid Token",
                detail: "Access token is invalid.",
                statusCode: StatusCodes.Status401Unauthorized);
        }

        var result = await db.RefreshTokens
            .AsNoTracking()
            .Where(rt => rt.Token == command.RefreshToken && rt.UserId == userId)
            .Select(rt => new
            {
                Token = rt,
                User  = db.Users.IgnoreQueryFilters().FirstOrDefault(u => u.Id == userId)
            })
            .FirstOrDefaultAsync(ct);

        if (result?.Token is null)
        {
            logger.LogWarning("Token refresh failed: refresh token not found for User {UserId}", userId);
            return TypedResults.Problem(
                title: "Invalid Token",
                detail: "Refresh token not found.",
                statusCode: StatusCodes.Status401Unauthorized);
        }

        if (!result.Token.IsValid)
        {
            logger.LogWarning("Token refresh failed: token is expired or revoked for User {UserId}", userId);
            return TypedResults.Problem(
                title: "Token Invalid",
                detail: "Refresh token is expired or revoked.",
                statusCode: StatusCodes.Status401Unauthorized);
        }

        if (result.User is null)
        {
            logger.LogError("Token refresh failed: User {UserId} not found despite valid token", userId);
            return TypedResults.Problem(
                title: "Invalid Token",
                detail: "User not found.",
                statusCode: StatusCodes.Status401Unauthorized);
        }

        // 3. Revoke the old token — re-fetch as tracked entity for the update
        var trackedToken = await db.RefreshTokens.FirstAsync(rt => rt.Token == command.RefreshToken, ct);
        trackedToken.Revoke();
        await db.SaveChangesAsync(ct);

        var token = await tokenProvider.GenerateTokenAsync(result.User, ct);
        if (token.IsError)
        {
            logger.LogError("Token generation failed for User {UserId}: {Error}", userId, token.TopError.Description);
            return TypedResults.Problem(
                title: token.TopError.Code,
                detail: token.TopError.Description,
                statusCode: StatusCodes.Status500InternalServerError);
        }

        logger.LogInformation("User {UserId} successfully refreshed tokens", userId);
        return TypedResults.Ok(token.Value);
    }
}
