using DeskForge.Api.Common.Enums;
using DeskForge.Api.Infrastructure.Auth.Models;
using DeskForge.Api.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Wolverine.Http;

namespace DeskForge.Api.Features.Tickets.Workflow;

[Tags("Ticket")]
public static class ResolveTicketEndpoint
{
    [Authorize]
    [WolverinePut("api/tickets/{ticketId}/resolve")]
    [EndpointSummary("ResolveTicket")]
    public static async Task<
            Results<Ok, NotFound, ProblemHttpResult, ForbidHttpResult>>
        Handle(
            [FromRoute] Guid ticketId,
            [FromServices] AppDbContext db,
            [FromServices] UserContext currentUser,
            CancellationToken ct)
    {
        var ticket = await db.Tickets
            .FirstOrDefaultAsync(t => t.Id == ticketId, ct);

        if (ticket is null)
            return TypedResults.NotFound();

        if (currentUser.Role == OrgRole.Staff &&
            ticket.AssignedToStaffId != currentUser.UserId)
        {
            return TypedResults.Forbid();
        }

        if (currentUser.Role == OrgRole.Requester)
        {
            return TypedResults.Forbid();
        }

        var result = ticket.Resolve();

        if (result.IsError)
        {
            return TypedResults.Problem(
                title: "Invalid Ticket Transition",
                detail: result.TopError.Description,
                statusCode: StatusCodes.Status400BadRequest);
        }

        await db.SaveChangesAsync(ct);

        return TypedResults.Ok();
    }
}