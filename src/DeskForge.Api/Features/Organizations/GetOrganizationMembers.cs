using DeskForge.Api.Common.Enums;
using DeskForge.Api.Common.Models;
using DeskForge.Api.Infrastructure.Auth.Models;
using DeskForge.Api.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Wolverine.Http;

namespace DeskForge.Api.Features.Organizations;

public sealed record OrganizationMemberResponse(
    string Id,
    string UserName,
    string Email,
    string FirstName,
    string LastName,
    string Role
);

[Tags("Organizations")]
public static class GetOrganizationMembersEndpoint
{
    [Authorize]
    [WolverineGet("api/organizations/members")]
    public static async Task<Ok<PagedResult<OrganizationMemberResponse>>> Handle(
        AppDbContext db,
        UserContext currentUser,
        CancellationToken ct,
        int pageNumber = 1,
        int pageSize = 1,
        string? role = null,
        string? excludeRole = null)
    {
        pageNumber = Math.Max(1, pageNumber);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var query = db.Users
            .AsNoTracking()
            .Where(u => u.OrganizationId == currentUser.OrganizationId);

        if (!string.IsNullOrEmpty(role) && Enum.TryParse(role, true, out OrgRole targetRole))
        {
            query = query.Where(u => u.Role == targetRole);
        }

        if (!string.IsNullOrEmpty(excludeRole) && Enum.TryParse(excludeRole, true, out OrgRole targetExcludeRole))
        {
            query = query.Where(u => u.Role != targetExcludeRole);
        }

        var totalCount = await query.CountAsync(ct);

        var members = await query
            .OrderBy(u => u.Role) // Group by role first
            .ThenBy(u => u.LastName) // Alphabetical sorting is mandatory before pagination!
            .ThenBy(u => u.FirstName)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .Select(u => new OrganizationMemberResponse(
                u.Id.ToString(),
                u.UserName!,
                u.Email!,
                u.FirstName,
                u.LastName,
                u.Role.ToString()
            ))
            .ToListAsync(ct);

        var result = new PagedResult<OrganizationMemberResponse>(members, pageNumber, pageSize, totalCount);

        return TypedResults.Ok(result);
    }
}