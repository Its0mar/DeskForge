using DeskForge.Api.Features.Auth.Models;
using DeskForge.Api.Infrastructure.Auth.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Identity;
using Wolverine.Http;

namespace DeskForge.Api.Features.Auth.Profile;

public sealed record GetEmployeeProfileResponse(
    Guid Id,
    string FirstName,
    string LastName,
    string Email,
    string Role,
    Guid OrganizationId);

[Tags("Auth")]

public static class GetEmployeeProfileEndpoint
{
    [Authorize(Roles = "Manager, Staff")]
    [WolverineGet("api/auth/profile")]
    [EndpointSummary("GetEmployeeProfile")]
    public static async Task<Results<Ok<GetEmployeeProfileResponse>, NotFound>> Handle(
        UserContext currentUser,
        UserManager<AppUser> userManager,
        CancellationToken ct)
    {
        var user = await userManager.FindByIdAsync(currentUser.UserId.ToString());

        if (user is null)
        {
            return TypedResults.NotFound();
        }
        
        var response = new GetEmployeeProfileResponse(user.Id, user.FirstName, user.LastName, user.Email!, user.Role.ToString(), user.OrganizationId);
        
        return TypedResults.Ok(response); 
    }
}

