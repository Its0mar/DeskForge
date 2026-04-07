using DeskForge.Api.Features.Auth.Models;
using DeskForge.Api.Infrastructure.Auth.Models;
using DeskForge.Api.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Wolverine.Http;

namespace DeskForge.Api.Features.Auth.Profile;

[Tags("Auth")]
public static class GetProfileEndpoint
{
    [Authorize]
    [WolverineGet("api/a")]
    [EndpointSummary("GetMyProfile")]
    public static async Task<Results<Ok<GetMyProfileResponse>, NotFound>> Handle(
        UserContext currentUser,
        AppDbContext db,
        CancellationToken ct)
    {

        var user = await db.Users
            .AsNoTracking()
            .IgnoreQueryFilters()
            .Where(u => u.Id == currentUser.UserId)
            .Select(u => new GetMyProfileResponse(
                u.Id.ToString(),
                u.UserName!,
                u.Email!,
                u.FirstName,
                u.LastName,
                u.Role.ToString(),
                u.OrganizationId.ToString(),
                u.Organization.Name ))
            .FirstOrDefaultAsync(ct);

        return user is not null
            ? TypedResults.Ok(user)
            : TypedResults.NotFound();
    }
}

public sealed record GetMyProfileResponse(
    string Id, 
    string UserName, 
    string Email, 
    string FirstName, 
    string LastName, 
    string Role, 
    string OrgId,
    string OrgName
);
