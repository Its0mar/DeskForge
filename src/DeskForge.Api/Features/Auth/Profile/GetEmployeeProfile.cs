using DeskForge.Api.Features.Auth.Models;
using DeskForge.Api.Infrastructure.Auth.Models;
using DeskForge.Api.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
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
public static class GetProfileEndpoint
{
    [Authorize]
    [WolverineGet("api/auth/profile")]
    [EndpointSummary("GetMyProfile")]
    public static async Task<Results<Ok<GetEmployeeProfileResponse>, NotFound>> Handle(
        UserContext currentUser,
        AppDbContext db,
        CancellationToken ct)
    {

        var user = await db.Users
            .AsNoTracking()
            .IgnoreQueryFilters()
            .Where(u => u.Id == currentUser.UserId)
            .Select(u => new GetEmployeeProfileResponse(
                u.Id,
                u.FirstName,
                u.LastName,
                u.Email!,
                u.Role.ToString(),
                u.OrganizationId))
            .FirstOrDefaultAsync(ct);

        return user is not null
            ? TypedResults.Ok(user)
            : TypedResults.NotFound();
    }
}
