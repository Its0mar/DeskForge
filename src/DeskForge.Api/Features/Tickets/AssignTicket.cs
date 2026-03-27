using DeskForge.Api.Common.Enums;
using DeskForge.Api.Features.Tickets.Events;
using DeskForge.Api.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Wolverine.Http;

namespace DeskForge.Api.Features.Tickets;

public sealed record AssignTicketRequest(Guid StaffId);

[Tags("Ticket")]
public static class AssignTicketEndpoint
{
    public static async Task<ProblemDetails> ValidateAsync([FromRoute] Guid ticketId, [FromBody]AssignTicketRequest request, AppDbContext db, CancellationToken ct)
    {
        var ticketExist = await db.Tickets.AnyAsync(t => t.Id == ticketId, ct);
        if (!ticketExist)
        {
            return new ProblemDetails
            {
                Status = StatusCodes.Status404NotFound,
                Title = "Ticket not found",
                Detail = "Ticket not found"
            };
        }

        var staffExist = await db.Users.AnyAsync(u => u.Id == request.StaffId && u.Role != OrgRole.Requester, ct);
        if (!staffExist)
        {
            return new ProblemDetails
            {
                Status = StatusCodes.Status404NotFound,
                Title = "Staff not found",
                Detail = "Staff not found"
            };
        }

        return WolverineContinue.NoProblems;
    }

    [Authorize(Policy = "OwnerOrManager")]
    [WolverinePut("api/Tickets/{ticketId}/AssignTicket")]
    [EndpointSummary("AssignTicket")]
    public static async Task<(Ok, TicketAssignedEvent)> Handle([FromRoute] Guid ticketId, [FromBody]AssignTicketRequest request, AppDbContext db, CancellationToken ct)
    {
        var ticket = await db.Tickets.FirstOrDefaultAsync(t => t.Id == ticketId, ct);
        ticket!.AssignTo(request.StaffId);
        
        await db.SaveChangesAsync(ct);
        return (TypedResults.Ok(), new TicketAssignedEvent(ticket.Id, request.StaffId));
    }
    
}