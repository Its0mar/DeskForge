using System.Security.Claims;
using DeskForge.Api.Common.Dtos;
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
        CancellationToken ct)
    {
        var principal = tokenProvider.GetPrincipalFromExpiredToken(command.AccessToken);
        if (principal is null)
        {
            return TypedResults.Problem(
                title: "Invalid Token",
                detail: "Access token is invalid.",
                statusCode: StatusCodes.Status401Unauthorized);
        }
        
        var userIdClaim = principal.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(userIdClaim, out var userId))
        {
            return TypedResults.Problem(
                title: "Invalid Token",
                detail: "Access token is invalid.",
                statusCode: StatusCodes.Status401Unauthorized);
        }

        var storedToken = await db.RefreshTokens.FirstOrDefaultAsync(rt =>
            rt.Token == command.RefreshToken &&
            rt.UserId == userId, ct);

        if (storedToken is null)
        {
            return TypedResults.Problem(
                title: "Invalid Token",
                detail: "Refresh token not found.",
                statusCode: StatusCodes.Status401Unauthorized);
        }

        if (!storedToken.IsValid)
        {
            return TypedResults.Problem(
                title: "Token Invalid",
                detail: "Refresh token is invalid.",
                statusCode: StatusCodes.Status401Unauthorized);
        }

        var user = await db.Users.FirstOrDefaultAsync(u => u.Id == userId, ct);
        if (user is null)
        {
            return TypedResults.Problem(
                title: "Invalid Token",
                detail: "User not found.",
                statusCode: StatusCodes.Status401Unauthorized);
        }
        
        //revoked token will be deleted 
        storedToken.Revoke();
        await db.SaveChangesAsync(ct);

        var token = await tokenProvider.GenerateTokenAsync(user, ct);
        if (token.IsError)
        {
            return TypedResults.Problem(
                title: token.TopError.Code,
                detail: token.TopError.Description,
                statusCode: StatusCodes.Status500InternalServerError);
        }

        return TypedResults.Ok(token.Value);
    }
}