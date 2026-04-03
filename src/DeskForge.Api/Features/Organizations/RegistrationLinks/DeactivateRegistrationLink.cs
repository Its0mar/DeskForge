using DeskForge.Api.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Wolverine.Http;

namespace DeskForge.Api.Features.Organizations.RegistrationLinks;

[Tags("Organizations")]
public static class DeactivateRegistrationLinkEndpoint
{
    [Authorize(Policy = "OwnerOnly")]
    [WolverinePut("api/organizations/registration-links/{linkId}/deactivate")]
    [EndpointSummary("Deactivate Registration Link")]
    public static async Task<Results<NoContent, NotFound>> Handle(
        Guid linkId,
        AppDbContext db,
        CancellationToken ct)
    {
        var link = await db.RegistrationLinks
            .FirstOrDefaultAsync(l => l.Id == linkId, ct);

        if (link is null)
            return TypedResults.NotFound();

        link.Deactivate();
        await db.SaveChangesAsync(ct);

        return TypedResults.NoContent();
    }
}