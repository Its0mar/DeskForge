using DeskForge.Api.Common.Enums;
using DeskForge.Api.Common.Models;
using DeskForge.Api.Infrastructure.Auth.Models;
using DeskForge.Api.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Wolverine.Http;

namespace DeskForge.Api.Features.Tickets.Comments;

public sealed record TicketCommentResponse(
    Guid Id,
    string Content,
    bool IsInternal,
    Guid CreatedByUserId,
    DateTimeOffset CreatedAtUtc);

[Tags("Comments")]
public static class GetTicketCommentsEndpoint
{
    [Authorize]
    [WolverineGet("api/tickets/{ticketId}/comments")]
    [EndpointSummary("GetTicketComments")]
    public static async Task<
        Results<Ok<PagedResult<TicketCommentResponse>>, NotFound, ForbidHttpResult>>
        Handle(
            [FromRoute] Guid ticketId,
            AppDbContext db,
            UserContext currentUser,
            CancellationToken ct,
            int pageNumber = 1,
            int pageSize = 20)
    {
        pageNumber = Math.Max(1, pageNumber);
        pageSize   = Math.Clamp(pageSize, 1, 100);

        var ticket = await db.Tickets
            .AsNoTracking()
            .Select(t => new { t.Id, t.AssignedToStaffId, t.SubmittedByUserId })
            .FirstOrDefaultAsync(t => t.Id == ticketId, ct);

        if (ticket is null)
            return TypedResults.NotFound();

        if (currentUser.Role == OrgRole.Staff &&
            ticket.AssignedToStaffId != currentUser.UserId)
        {
            return TypedResults.Forbid();
        }

        if (currentUser.Role == OrgRole.Requester &&
            ticket.SubmittedByUserId != currentUser.UserId)
        {
            return TypedResults.Forbid();
        }

        var query = db.TicketComments
            .AsNoTracking()
            .Where(c => c.TicketId == ticketId);

        // Requesters cannot see internal notes
        if (currentUser.Role == OrgRole.Requester)
            query = query.Where(c => !c.IsInternal);

        var totalCount = await query.CountAsync(ct);

        var comments = await query
            .OrderBy(c => c.CreatedAtUtc)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .Select(c => new TicketCommentResponse(
                c.Id,
                c.Content,
                c.IsInternal,
                c.CreatedById ?? Guid.Empty,
                c.CreatedAtUtc))
            .ToListAsync(ct);

        return TypedResults.Ok(new PagedResult<TicketCommentResponse>(comments, pageNumber, pageSize, totalCount));
    }
}