using System.Diagnostics.CodeAnalysis;
using DeskForge.Api.Common.Enums;
using DeskForge.Api.Infrastructure.Auth.Models;
using DeskForge.Api.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Wolverine.Http;

namespace DeskForge.Api.Features.Tickets;

[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)]
// public sealed record GetTicketRequest([FromRoute] Guid TicketId);


[Tags("Ticket")]
public static class GetTicketEndpoint
{
    public static async Task<ProblemDetails> ValidateAsync(
        [FromRoute] Guid ticketId,
        AppDbContext db,
        UserContext currentUser,
        CancellationToken ct)
    {
        var ticket = await db.Tickets
            .Select(t => new {t.Id, t.AssignedToStaffId, t.SubmittedByUserId})
            .FirstOrDefaultAsync(t => t.Id == ticketId, ct);

        if (ticket is null)
        {
            return new ProblemDetails
            {
                Status = StatusCodes.Status404NotFound,
                Title = "Ticket not found",
                Detail = "Ticket not found"
            };
        }

        if (currentUser.Role == OrgRole.Staff && ticket.AssignedToStaffId != currentUser.UserId)
        {
            return new ProblemDetails
            {
                Status = StatusCodes.Status403Forbidden,
                Title = "Forbidden",
                Detail = "You are not assigned to this ticket."
            };
        }

        if (currentUser.Role == OrgRole.Requester && ticket.SubmittedByUserId != currentUser.UserId)
        {
            return new ProblemDetails
            {
                Status = StatusCodes.Status403Forbidden,
                Title = "Forbidden",
                Detail = "You did not submit this ticket."
            };
        }

        return WolverineContinue.NoProblems;
    }

    [Authorize]
    [WolverineGet("api/tickets/{ticketId}")]
    [EndpointSummary("GetTicket")]
    public static async Task<Results<Ok<GetTicketResponse>, NotFound>> Handle(
        [FromRoute] Guid ticketId,
        AppDbContext db,
        CancellationToken ct)
    {
        var ticket = await db.Tickets
            .Where(t => t.Id == ticketId)
            .Select(t => new GetTicketResponse(
                t.Id,
                t.Title,
                t.Description,
                t.Priority.ToString(),
                t.Status.ToString(),
                t.CategoryId,
                t.AssignedToStaffId,
                t.SubmitterName,
                t.SubmitterEmail,
                t.ResponseDeadline,
                t.ResolutionDeadline,
                t.CreatedAtUtc))
            .FirstOrDefaultAsync(ct);
        
        return ticket is not null 
            ? TypedResults.Ok(ticket) 
            : TypedResults.NotFound();
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