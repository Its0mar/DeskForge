using DeskForge.Api.Common.Enums;
using DeskForge.Api.Infrastructure.Auth.Models;
using DeskForge.Api.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Wolverine.Http;

namespace DeskForge.Api.Features.Tickets.Queries;

public sealed record TicketStatsResponse(
    int Total,
    int New,
    int InProgress,
    int Resolved,
    int Closed,
    int Overdue);

[Tags("Ticket")]
public static class GetTicketStatsEndpoint
{
    [Authorize]
    [WolverineGet("api/tickets/stats")]
    [EndpointSummary("GetTicketStats")]
    public static async Task<Ok<TicketStatsResponse>> Handle(
        AppDbContext db,
        UserContext currentUser,
        CancellationToken ct)
    {
        var query = db.Tickets.AsQueryable();

        // Role filtering
        switch (currentUser.Role)
        {
            case OrgRole.Requester:
                query = query.Where(t =>
                    t.SubmittedByUserId == currentUser.UserId);
                break;

            case OrgRole.Staff:
                query = query.Where(t =>
                    t.AssignedToStaffId == currentUser.UserId);
                break;
        }

        var now = DateTime.UtcNow;

        var stats = await query
            .GroupBy(_ => 1)
            .Select(g => new TicketStatsResponse(
                 g.Count(),

                g.Count(t =>
                    t.Status == TicketStatus.New),

                g.Count(t =>
                    t.Status == TicketStatus.InProgress),

                g.Count(t =>
                    t.Status == TicketStatus.Resolved),

                g.Count(t =>
                    t.Status == TicketStatus.Closed),

                g.Count(t =>
                    t.ResolutionDeadline < now &&
                    t.Status != TicketStatus.Closed &&
                    t.Status != TicketStatus.Resolved)
            ))
            .FirstOrDefaultAsync(ct);

        return TypedResults.Ok(stats);
    }
}