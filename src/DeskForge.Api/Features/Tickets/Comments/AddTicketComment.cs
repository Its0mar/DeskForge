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

namespace DeskForge.Api.Features.Tickets.Comments;

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
    [Authorize]
    [WolverinePost("api/tickets/{ticketId}/comments")]
    public static async Task<Results<Ok<Guid>, NotFound, ForbidHttpResult, BadRequest<string>>> Handle(
        [FromRoute] Guid ticketId,
        AddCommentRequest request,
        AppDbContext db,
        UserContext currentUser,
        CancellationToken ct)
    {
        var ticket = await db.Tickets
            .FirstOrDefaultAsync(t => t.Id == ticketId, ct);

        if (ticket is null)
            return TypedResults.NotFound();

        // closed ticket rule
        if (ticket.Status == TicketStatus.Closed)
            return TypedResults.BadRequest("Cannot add comments to a closed ticket.");

        // internal comment rule
        if (request.IsInternal && currentUser.Role == OrgRole.Requester)
            return TypedResults.Forbid();

        var comment = TicketComment.Create(
            request.Content,
            request.IsInternal,
            ticketId,
            currentUser.UserId);

        db.TicketComments.Add(comment);
        ticket.UpdateLastActivity();

        await db.SaveChangesAsync(ct);

        return TypedResults.Ok(comment.Id);
    }
    
}
