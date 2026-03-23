using DeskForge.Api.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Wolverine.Http;

namespace DeskForge.Api.Features.Categories;

[Tags("Category")]
public static class GetCategoriesEndpoint
{
    [Authorize(Policy = "OwnerOrManager")]
    [WolverineGet("api/categories")]
    [EndpointSummary("Retrieve all categories and their assigned teams for the organization")]
    public static async Task<Ok<IReadOnlyList<GetCategoryResponse>>> Handle(AppDbContext db, CancellationToken ct)
    {
        var categories = await db.Categories.OrderBy(c => c.Name)
            .Select(c => new GetCategoryResponse(c.Id, c.Name, c.Description,  c.TargetTeam.Name, c.TargetTeamId ))
            .ToListAsync(ct);

        return TypedResults.Ok<IReadOnlyList<GetCategoryResponse>>(categories);
    }
}

public sealed record GetCategoryResponse(Guid Id, string Name, string? Description, string TargetTeamName, Guid TargetTeamId );