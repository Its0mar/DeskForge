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
    public static async Task<ProblemDetails?> ValidateAsync([FromRoute] Guid ticketId, [FromBody]AssignTicketRequest request, AppDbContext db, CancellationToken ct)
    {
        var staffExists = await db.Users
            .AsNoTracking()
            .AnyAsync(u => u.Id == request.StaffId && u.Role != OrgRole.Requester, ct);
        if (!staffExists)
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
    [WolverinePut("api/tickets/{ticketId}/assign")]
    [EndpointSummary("AssignTicket")]
    public static async Task<(Results<Ok, ProblemHttpResult>, TicketAssignedEvent?)> Handle([FromRoute] Guid ticketId, [FromBody]AssignTicketRequest request, AppDbContext db, CancellationToken ct)
    {
        var ticket = await db.Tickets.FirstOrDefaultAsync(t => t.Id == ticketId, ct);

        if (ticket is null)
        {
            return (
                TypedResults.Problem(            
                    statusCode : StatusCodes.Status404NotFound,
                    title: "Ticket not found",
                    detail: "Ticket not found"),
                null
                );
        }
        var result = ticket.AssignTo(request.StaffId);
        
        if (result.IsError)
        {
            return (
                TypedResults.Problem(
                    statusCode: StatusCodes.Status409Conflict,
                    title: result.TopError.Code,
                    detail: result.TopError.Description),
                null
            );
        }
        
        await db.SaveChangesAsync(ct);
        return (TypedResults.Ok(), new TicketAssignedEvent(ticket.Id, request.StaffId));
    }
    
}