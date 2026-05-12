using DeskForge.Api.Common.Extensions;
using DeskForge.Api.Common.Models;
using DeskForge.Api.Infrastructure.Auth.Models;
using DeskForge.Api.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Wolverine.Http;

namespace DeskForge.Api.Features.Organizations.Invitations;

[Tags("Invitations")]
public class GetInvitesEndpoint
{
    [Authorize(Roles = "Owner, Manager")]
    [WolverineGet("api/organizations/invites")]
    public async Task<Ok<PagedResult<GetInvitesResponse>>> Handle(
        UserContext currentUser,
        AppDbContext db,
        CancellationToken ct,
        int pageNumber = 1,
        int pageSize = 1)
    {
        pageNumber = Math.Max(1, pageNumber);
        pageSize = Math.Clamp(pageSize, 1, 100);
        
        var orgId = currentUser.OrganizationId;
        var invites = await db.Invitations.Where(i => 
                i.OrganizationId == orgId &&
                i.IsActive &&
                i.ExpiresAtUtc > DateTimeOffset.UtcNow)
            .AsNoTracking()
            .Select(i => new GetInvitesResponse
            (
                i.Id,
                i.Email,
                i.Role.ToString(),
                i.CreatedBy != null ? i.CreatedBy.UserName! : "N/A"
            )).ToPagedResultAsync(pageNumber, pageSize, ct);

        return TypedResults.Ok(invites);
    }
}

public record GetInvitesResponse(Guid Id, string Email, string Role, string InvitedBy);