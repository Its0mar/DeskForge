using DeskForge.Api.Common.Extensions;
using DeskForge.Api.Common.Models;
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
    public static async Task<Ok<PagedResult<GetCategoryResponse>>> Handle(
        AppDbContext db,
        CancellationToken ct,
        [FromQuery] int page = 1,
        [FromQuery] int size = 10)
    {
        var pageSize = size > 50 ? 50 : size;
        
        var query = db.Categories
            .AsNoTracking()
            .OrderBy(c => c.Name)
            .Select(c => new GetCategoryResponse(
                c.Id, 
                c.Name, 
                c.Description,  
                c.TargetTeam.Name, 
                c.TargetTeamId));
        
        var result = await query.ToPagedResultAsync(page, pageSize, ct);
        
        return TypedResults.Ok(result);
    }
}

public sealed record GetCategoryResponse(Guid Id, string Name, string? Description, string TargetTeamName, Guid TargetTeamId );