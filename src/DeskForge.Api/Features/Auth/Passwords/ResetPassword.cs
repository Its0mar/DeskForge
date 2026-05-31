using DeskForge.Api.Features.Auth.Models;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Wolverine.Http;

namespace DeskForge.Api.Features.Auth.Passwords;

public record ResetPasswordCommand(
    string Email, 
    string Token, 
    string NewPassword, 
    string ConfirmPassword);
    
public sealed class ResetPasswordCommandValidator : AbstractValidator<ResetPasswordCommand>
{
    public ResetPasswordCommandValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress();
        
        RuleFor(x => x.Token).NotEmpty().WithMessage("Reset token is required.");
        
        RuleFor(x => x.NewPassword)
            .NotEmpty()
            .MinimumLength(8).WithMessage("Password must be at least 8 characters long.");
            
        RuleFor(x => x.ConfirmPassword)
            .Equal(x => x.NewPassword).WithMessage("Passwords do not match.");
    }
}

[Tags("Auth")]
public static class ResetPasswordEndpoint
{
    [AllowAnonymous]
    [WolverinePost("api/auth/reset-password")]
    [EndpointSummary("Reset forgotten password using email token")]
    public static async Task<Results<NoContent, ProblemHttpResult>> Handle(
        ResetPasswordCommand command,
        UserManager<AppUser> userManager)
    {
        var user = await userManager.Users
            .IgnoreQueryFilters()
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Email == command.Email);


        if (user is null)
        {
            return TypedResults.Problem(
                statusCode: StatusCodes.Status400BadRequest,
                detail: "Invalid email or token.");
        }

        var result = await userManager.ResetPasswordAsync(user, command.Token, command.NewPassword);

        if (!result.Succeeded)
        {
            var errors = string.Join(", ", result.Errors.Select(e => e.Description));
            
            return TypedResults.Problem(
                statusCode: StatusCodes.Status400BadRequest,
                title: "Password reset failed",
                detail: errors);
        }

        return TypedResults.NoContent();
    }
}