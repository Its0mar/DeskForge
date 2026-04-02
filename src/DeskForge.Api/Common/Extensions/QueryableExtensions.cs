using DeskForge.Api.Common.Models;
using Microsoft.EntityFrameworkCore;

namespace DeskForge.Api.Common.Extensions;

public static class QueryableExtensions
{
    public static async Task<PagedResult<T>> ToPagedResultAsync<T>(
        this IQueryable<T> query, 
        int pageNumber, 
        int pageSize, 
        CancellationToken ct)
    {
        var totalCount = await query.CountAsync(ct);
        var items = await query
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return new PagedResult<T>(items, pageNumber, pageSize, totalCount);
    }
}