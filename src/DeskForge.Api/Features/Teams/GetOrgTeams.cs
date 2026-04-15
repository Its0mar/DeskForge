using DeskForge.Api.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Wolverine.Http;

namespace DeskForge.Api.Features.Teams;

[Tags("Teams")]
public static class GetOrgTeamsEndpoint
{
    [Authorize(Policy = "OwnerOrManager")]
    [WolverineGet("api/organizations/teams")]
    [EndpointSummary("GetOrgTeams")]
    public static async Task<Ok<IReadOnlyList<GetOrgTeamsResponse>>> Handle(AppDbContext db, CancellationToken ct)
    {
        var teams = await db.Teams
            .OrderBy(t => t.Name)
            .Select(t => new GetOrgTeamsResponse(
                t.Id,
                t.Name,
                t.Members.Count))
            .ToListAsync(ct);
        
        return TypedResults.Ok<IReadOnlyList<GetOrgTeamsResponse>>(teams);
        
    }
}


public sealed record GetOrgTeamsResponse(Guid TeamId, string TeamName, int NumberOfMembers);