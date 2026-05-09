using DeskForge.Api.Features.Auth.Models;
using DeskForge.Api.Infrastructure.Auth.Models;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Identity;
using Wolverine.Http;

namespace DeskForge.Api.Features.Auth;

public record ChangePasswordCommand(
    string CurrentPassword, 
    string NewPassword, 
    string ConfirmPassword);
    
    
public sealed class ChangePasswordCommandValidator : AbstractValidator<ChangePasswordCommand>
{
    public ChangePasswordCommandValidator()
    {
        RuleFor(x => x.CurrentPassword)
            .NotEmpty().WithMessage("Current password is required.");
            
        RuleFor(x => x.NewPassword)
            .NotEmpty()
            .MinimumLength(8).WithMessage("New password must be at least 8 characters long.")
            .NotEqual(x => x.CurrentPassword).WithMessage("New password must be different from current password.");
            
        RuleFor(x => x.ConfirmPassword)
            .Equal(x => x.NewPassword).WithMessage("Passwords do not match.");
    }
}

[Tags("Profile")]
public static class ChangePasswordEndpoint
{
    [Authorize]
    [WolverinePut("api/profile/password")]
    [EndpointSummary("Change password for authenticated user")]
    public static async Task<Results<NoContent, ProblemHttpResult>> Handle(
        ChangePasswordCommand command,
        UserContext currentUser,
        UserManager<AppUser> userManager)
    {
        var user = await userManager.FindByIdAsync(currentUser.UserId.ToString());
        
        if (user is null)
        {
            return TypedResults.Problem(
                statusCode: StatusCodes.Status404NotFound, 
                detail: "User not found.");
        }

        var result = await userManager.ChangePasswordAsync(user, command.CurrentPassword, command.NewPassword);

        if (!result.Succeeded)
        {
            var errors = string.Join(", ", result.Errors.Select(e => e.Description));
            
            return TypedResults.Problem(
                statusCode: StatusCodes.Status400BadRequest,
                title: "Password update failed",
                detail: errors);
        }

        return TypedResults.NoContent();
    }
}