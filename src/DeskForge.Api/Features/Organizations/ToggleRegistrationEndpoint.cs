using DeskForge.Api.Infrastructure.Auth.Models;
using DeskForge.Api.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Wolverine.Http;

namespace DeskForge.Api.Features.Organizations;

[Tags("Organizations")]
public class ToggleRegistrationEndpoint
{
    [Authorize(Policy = "OwnerOrManager")]
    [WolverinePut("api/organizations/registration/open")]
    [EndpointSummary("Open Public Registration")]
    public static async Task<Results<NoContent, NotFound>> HandleOpen(
        [NotBody] UserContext currentUser,
        [NotBody] AppDbContext db,
        [NotBody] CancellationToken ct)
    {
        var org = await db.Organizations
            .FirstOrDefaultAsync(o => o.Id == currentUser.OrganizationId, ct);

        if (org is null)
            return TypedResults.NotFound();

        org.OpenRegistration();
        await db.SaveChangesAsync(ct);

        return TypedResults.NoContent();
    }

    [Authorize(Policy = "OwnerOrManager")]
    [WolverinePut("api/organizations/registration/close")]
    [EndpointSummary("Close Public Registration")]
    public static async Task<Results<NoContent, NotFound>> HandleClose(
        [NotBody] UserContext currentUser,
        [NotBody] AppDbContext db,
        [NotBody] CancellationToken ct)
    {
        var org = await db.Organizations
            .FirstOrDefaultAsync(o => o.Id == currentUser.OrganizationId, ct);

        if (org is null)
            return TypedResults.NotFound();

        org.CloseRegistration();
        await db.SaveChangesAsync(ct);

        return TypedResults.NoContent();
    }
}