using System.Diagnostics.CodeAnalysis;
using DeskForge.Api.Common.Enums;
using DeskForge.Api.Features.Tickets.Models;
using DeskForge.Api.Infrastructure.Auth.Models;
using DeskForge.Api.Infrastructure.Persistence;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Wolverine.Http;

namespace DeskForge.Api.Features.Tickets;

public sealed record AddCommentRequest(string Content, bool IsInternal = false);

public sealed record AddTicketCommentCommand(
    [FromRoute] Guid TicketId, 
    [FromBody] AddCommentRequest Request);

public sealed class AddCommentRequestValidator 
    : AbstractValidator<AddCommentRequest>
{
    public AddCommentRequestValidator()
    {
        RuleFor(x => x.Content)
            .NotEmpty().WithMessage("Message is required.")
            .MaximumLength(2000).WithMessage("Message cannot exceed 2000 characters.");
    }
}

[Tags("comments")]
public static class AddTicketCommentEndpoint
{
    public static async Task<ProblemDetails> ValidateAsync(
        AddTicketCommentCommand command,
        AppDbContext db,
        UserContext currentUser,
        CancellationToken ct)
    {
        var ticket = await db.Tickets
            .FirstOrDefaultAsync(t => t.Id == command.TicketId, ct);

        if (ticket is null)
            return new ProblemDetails
            {
                Status = StatusCodes.Status404NotFound,
                Title  = "Not Found",
                Detail = "Ticket not found."
            };

        if (ticket.Status == TicketStatus.Closed)
            return new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title  = "Ticket Closed",
                Detail = "Cannot add comments to a closed ticket."
            };

        if (currentUser.Role == OrgRole.Requester && command.Request.IsInternal)
            return new ProblemDetails
            {
                Status = StatusCodes.Status403Forbidden,
                Title  = "Forbidden",
                Detail = "Requesters cannot add internal notes."
            };

        return WolverineContinue.NoProblems;
    }

    [Authorize]
    [WolverinePost("api/tickets/{ticketId}/comments")]
    [EndpointSummary("AddComment")]
    public static async Task<Ok<Guid>> Handle(
        AddTicketCommentCommand command,
        AppDbContext db,
        UserContext currentUser,
        CancellationToken ct)
    {
        var comment = TicketComment.Create(command.Request.Content, command.Request.IsInternal, command.TicketId, currentUser.UserId);
        
        db.TicketComments.Add(comment);
        await db.Tickets.Where(t => t.Id == command.TicketId)
            .ExecuteUpdateAsync(s => s.SetProperty(t => t.LastActivityAt, DateTimeOffset.UtcNow), ct);
       
        await db.SaveChangesAsync(ct);
        
        return TypedResults.Ok(comment.Id);
    }
    
}
