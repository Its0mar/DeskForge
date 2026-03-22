using DeskForge.Api.Infrastructure.Auth.Models;
using DeskForge.Api.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Wolverine.Http;

namespace DeskForge.Api.Features.Auth;

public record LogoutCommand(string RefreshToken);

[Tags("Auth")]
public static class LogoutEndpoint
{
    [Authorize]
    [WolverinePost("api/auth/logout")]
    [EndpointSummary("Logout")]
    public static async Task<Results<NoContent, NotFound>> Handle(
        LogoutCommand command,
        UserContext currentUser,
        AppDbContext db,
        CancellationToken ct)
    {
        var token = await db.RefreshTokens.FirstOrDefaultAsync(rt => rt.Token == command.RefreshToken && rt.IsValid, ct);

        if (token is null || token.UserId != currentUser.UserId)
        {
            return TypedResults.NotFound();
        }
        
        token.Revoke();
        await db.SaveChangesAsync(ct);
        
        return TypedResults.NoContent();
    }
}