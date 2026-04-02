using DeskForge.Api.Infrastructure.Auth.Models;
using DeskForge.Api.Infrastructure.Persistence;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Wolverine.Http;

namespace DeskForge.Api.Features.Auth;

public record LogoutCommand(string RefreshToken);

public sealed class LogoutCommandValidator : AbstractValidator<LogoutCommand>
{
    public LogoutCommandValidator()
    {
        RuleFor(x => x.RefreshToken).NotEmpty().WithMessage("Refresh token is required");
    }
}

[Tags("Auth")]
public static class LogoutEndpoint
{
    [Authorize]
    [WolverinePost("api/auth/logout")]
    [EndpointSummary("Logout")]
    public static async Task<NoContent> Handle(
        LogoutCommand command,
        UserContext currentUser,
        AppDbContext db,
        ILogger<LogoutCommand> logger,
        CancellationToken ct)
    {
        var token = await db.RefreshTokens
            .FirstOrDefaultAsync(rt => rt.Token == command.RefreshToken && rt.IsValid, ct);

        if (token is null || token.UserId != currentUser.UserId)
        {
            logger.LogWarning(
                "Logout called with invalid or unowned token for User {UserId}",
                currentUser.UserId);
            return TypedResults.NoContent();
        }
        
        token.Revoke();
        await db.SaveChangesAsync(ct);

        logger.LogInformation("User {UserId} logged out successfully", currentUser.UserId);
        return TypedResults.NoContent();
    }
}