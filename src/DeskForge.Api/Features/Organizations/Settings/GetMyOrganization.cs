using DeskForge.Api.Infrastructure.Auth.Models;
using DeskForge.Api.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Wolverine.Http;

namespace DeskForge.Api.Features.Organizations;



[Tags("Organizations")]
public static class GetOrganizationEndpoint
{
    [Authorize(Policy = "OwnerOrManager")]
    [WolverineGet("api/organizations")]
    [EndpointSummary("GetMyOrg")]
    public static async Task<Results<Ok<GetOrganizationQueryResponse>, NotFound>> Handle( AppDbContext db, UserContext currentUser, CancellationToken ct)
    {
        var organization = await db.Organizations
            .AsNoTracking()
            .FirstOrDefaultAsync(o => o.Id == currentUser.OrganizationId, ct);

        if (organization is null)
        {
            return TypedResults.NotFound();
        }

        var orgResponse = new GetOrganizationQueryResponse(organization.Id, organization.Name, organization.TenantCode);
        return TypedResults.Ok(orgResponse);
    }
}

public sealed record GetOrganizationQueryResponse(Guid Id, string Name, string TenantCode);