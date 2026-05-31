using DeskForge.Api.Common.Enums;
using DeskForge.Api.Common.Models;
using DeskForge.Api.Infrastructure.Auth.Models;
using DeskForge.Api.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Wolverine.Http;

namespace DeskForge.Api.Features.Tickets.Queries;

public sealed record TicketListItem(
    Guid Id,
    string Title,
    string Status,
    string Priority,
    string SubmitterName,
    Guid? AssignedToStaffId,
    DateTimeOffset CreatedAtUtc);

[Tags("Ticket")]
public static class GetTicketsEndpoint
{
    [Authorize]
    [WolverineGet("api/tickets")]
    [EndpointSummary("GetTickets")]
    public static async Task<Ok<PagedResult<TicketListItem>>> Handle(
        AppDbContext db,
        UserContext currentUser,
        CancellationToken ct,
        int pageNumber = 1,
        int pageSize = 20,
        TicketStatus? status = null,
        TicketPriority? priority = null,
        Guid? assignedTo = null,
        Guid? categoryId = null)
    {
        pageNumber = Math.Max(1, pageNumber);
        pageSize   = Math.Clamp(pageSize, 1, 100);

        var query = db.Tickets.AsNoTracking().AsQueryable();

        switch (currentUser.Role)
        {
            case OrgRole.Requester:
                query = query.Where(t => t.SubmittedByUserId == currentUser.UserId);
                break;

            case OrgRole.Staff:
                query = query.Where(t => t.AssignedToStaffId == currentUser.UserId);
                break;
        }

        if (status.HasValue)
            query = query.Where(t => t.Status == status.Value);

        if (priority.HasValue)
            query = query.Where(t => t.Priority == priority.Value);

        if (assignedTo.HasValue)
            query = query.Where(t => t.AssignedToStaffId == assignedTo.Value);

        if (categoryId.HasValue)
            query = query.Where(t => t.CategoryId == categoryId.Value);

        var totalCount = await query.CountAsync(ct);

        var tickets = await query
            .OrderByDescending(t => t.CreatedAtUtc)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .Select(t => new TicketListItem(
                t.Id,
                t.Title,
                t.Status.ToString(),
                t.Priority.ToString(),
                t.SubmitterName,
                t.AssignedToStaffId,
                t.CreatedAtUtc))
            .ToListAsync(ct);

        return TypedResults.Ok(new PagedResult<TicketListItem>(tickets, pageNumber, pageSize, totalCount));
    }
}