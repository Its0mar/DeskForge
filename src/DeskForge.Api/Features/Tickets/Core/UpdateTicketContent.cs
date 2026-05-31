using DeskForge.Api.Common.Enums;
using DeskForge.Api.Infrastructure.Auth.Models;
using DeskForge.Api.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Wolverine.Http;

namespace DeskForge.Api.Features.Tickets.Core;

public sealed record UpdateTicketContentRequest(
    string Title,
    string Description);

[Tags("Ticket")]
public static class UpdateTicketContentEndpoint
{
    [Authorize]
    [WolverinePut("api/tickets/{ticketId}/content")]
    [EndpointSummary("UpdateTicketContent")]
    public static async Task<
            Results<Ok, NotFound, ProblemHttpResult, ForbidHttpResult>>
        Handle(
            [FromRoute] Guid ticketId,
            UpdateTicketContentRequest request,
            AppDbContext db,
            UserContext currentUser,
            CancellationToken ct)
    {
        var ticket = await db.Tickets
            .FirstOrDefaultAsync(t => t.Id == ticketId, ct);

        if (ticket is null)
            return TypedResults.NotFound();

        if (currentUser.Role == OrgRole.Requester)
            return TypedResults.Forbid();

        if (currentUser.Role == OrgRole.Staff &&
            ticket.AssignedToStaffId != currentUser.UserId)
        {
            return TypedResults.Forbid();
        }

        if (ticket.Status == TicketStatus.Closed)
        {
            return TypedResults.Problem(
                title: "Ticket Closed",
                detail: "Closed tickets cannot be edited.",
                statusCode: StatusCodes.Status400BadRequest);
        }

        ticket.UpdateContent(
            request.Title,
            request.Description);

        await db.SaveChangesAsync(ct);

        return TypedResults.Ok();
    }
}