using DeskForge.Api.Infrastructure.Persistence;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Wolverine.Http;

namespace DeskForge.Api.Features.Organizations;

public sealed record GetOrganizationQueryResponse(Guid Id, string Name, string TenantCode);

[Tags("Organizations")]
public static class GetOrganizationEndpoint
{
    [WolverineGet("api/organizations/{id:guid}")]
    [EndpointSummary("GetOrganizationById")]
    public static async Task<Results<Ok<GetOrganizationQueryResponse>, NotFound>> Handle(Guid id, AppDbContext context, CancellationToken ct)
    {
        var organization = await context.Organizations
            .AsNoTracking()
            .FirstOrDefaultAsync(o => o.Id == id, ct);

        if (organization is null)
        {
            return TypedResults.NotFound();
        }

        var orgResponse = new GetOrganizationQueryResponse(organization.Id, organization.Name, organization.TenantCode);
        return TypedResults.Ok(orgResponse);
    }
}