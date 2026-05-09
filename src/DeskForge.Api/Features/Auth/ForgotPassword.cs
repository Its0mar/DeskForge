using DeskForge.Api.Features.Auth.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Wolverine;
using Wolverine.Http;

namespace DeskForge.Api.Features.Auth;

public sealed record ForgotPasswordRequest(string Identifier);

public sealed record PasswordResetRequestedEvent(Guid UserId, string Email, string Token);


[Tags("Auth")]
public static class ForgotPassword
{
    [AllowAnonymous]
    [WolverinePost("api/auth/forgot-password")]
    public static async Task<NoContent> Handle(
        ForgotPasswordRequest request, 
        UserManager<AppUser> userManager,
        IMessageBus bus)
    {
        
        var user = await userManager.Users
            .IgnoreQueryFilters()
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Email == request.Identifier || u.UserName == request.Identifier);

        if (user is null)
        {
            return TypedResults.NoContent();
        }

        var token = await userManager.GeneratePasswordResetTokenAsync(user);

        await bus.PublishAsync(new PasswordResetRequestedEvent(user.Id, user.Email!, token));
        
        return TypedResults.NoContent();
    }
}