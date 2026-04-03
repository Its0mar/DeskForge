using DeskForge.Api.Common.Extensions;
using DeskForge.Api.Common.Models;
using DeskForge.Api.Infrastructure.Auth.Models;
using DeskForge.Api.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Wolverine.Http;

namespace DeskForge.Api.Features.Organizations.RegistrationLinks;


[Tags("Organizations")]
public class GetRegistrationLinksEndpoint
{
    [Authorize(Policy = "OwnerOrManager")]
    [WolverineGet("api/organizations/registration-links")]
    [EndpointSummary("Get Registration Links")]
    public static async Task<Ok<PagedResult<RegistrationLinkResponse>>> Handle(
        AppDbContext db,
        UserContext currentUser,
        CancellationToken ct,
        [FromQuery] int page,
        [FromQuery] int size)
    {
        if (page <= 0 || size <= 0)
        {
            page = 1;
            size = 10;
        }
        var query = db.RegistrationLinks
            .AsNoTracking()
            .OrderBy(q => q.CreatedAtUtc)
            .Select(q => new RegistrationLinkResponse(q.Id, q.Token, q.ExpiresAt, q.MaxUsage, q.UsageCount, q.IsActive, q.IsValid) );
        
        var result = await query.ToPagedResultAsync(page, size, ct);

        return TypedResults.Ok(result);
    }
}


public sealed record RegistrationLinkResponse(
    Guid Id,
    string Token,
    DateTime? ExpiresAt,
    int? MaxUsages,
    int UsageCount,
    bool IsActive,
    bool IsValid);