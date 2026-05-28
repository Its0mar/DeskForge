using DeskForge.Api.Common.Enums;
using DeskForge.Api.Features.Tickets.Models;
using DeskForge.Api.Infrastructure.Auth.Models;
using DeskForge.Api.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Wolverine.Http;

namespace DeskForge.Api.Features.Tickets;

[Tags("Ticket")]
public static class GetTicketEndpoint
{
    [Authorize]
    [WolverineGet("api/tickets/{ticketId}")]
    [EndpointSummary("GetTicket")]
    public static async Task<Results<Ok<GetTicketResponse>, NotFound, ForbidHttpResult>> Handle(
        [FromRoute] Guid ticketId,
        AppDbContext db,
        UserContext currentUser,
        CancellationToken ct)
    {
        var ticket = await db.Tickets
            .Where(t => t.Id == ticketId)
            .FirstOrDefaultAsync(ct);

        if (ticket is null)
            return TypedResults.NotFound();
        
        if (!CanAccess(ticket, currentUser))
            return TypedResults.Forbid();

        var response = ToTicketResponse(ticket);

        return TypedResults.Ok(response);
    }
    
    private static bool CanAccess(Ticket ticket, UserContext user)
    {
        return user.Role switch
        {
            OrgRole.Manager => true,

            OrgRole.Staff =>
                ticket.AssignedToStaffId == user.UserId,

            OrgRole.Requester =>
                ticket.SubmittedByUserId == user.UserId,

            _ => false
        };
    }
    private static GetTicketResponse ToTicketResponse(Ticket ticket)
    {
        return new GetTicketResponse(
            ticket.Id,
            ticket.Title,
            ticket.Description,
            ticket.Priority.ToString(),
            ticket.Status.ToString(),
            ticket.CategoryId,
            ticket.AssignedToStaffId,
            ticket.SubmitterName,
            ticket.SubmitterEmail,
            ticket.ResponseDeadline,
            ticket.ResolutionDeadline,
            ticket.CreatedAtUtc
        );
    }
    
}


public sealed record GetTicketResponse(
    Guid Id,
    string Title,
    string Description,
    string Priority,
    string Status,
    Guid CategoryId,
    Guid? AssignedToStaffId,
    string SubmitterName,
    string SubmitterEmail,
    DateTimeOffset? ResponseDeadline,
    DateTimeOffset? ResolutionDeadline,
    DateTimeOffset CreatedAt);