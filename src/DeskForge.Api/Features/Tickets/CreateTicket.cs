using System.Diagnostics.CodeAnalysis;
using DeskForge.Api.Common.Enums;
using DeskForge.Api.Features.Sla;
using DeskForge.Api.Features.Tickets.Events;
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

[DynamicallyAccessedMembers((DynamicallyAccessedMemberTypes.PublicConstructors))]
public sealed record CreateTicketCommand(string Title, string Description, TicketPriority Priority, Guid CategoryId);

public sealed class CreateTicketCommandValidator : AbstractValidator<CreateTicketCommand>
{
    public CreateTicketCommandValidator()
    {
        RuleFor(x => x.Title).Length(3,50).WithMessage("Title must be between 3 and 50 characters long.");
        RuleFor(x => x.Description).Length(20,1000).WithMessage("Description must be between 20 and 1000 characters long.");
        RuleFor(x => x.Priority).IsInEnum().WithMessage("Priority is invalid.");
    }
}

[Tags("Ticket")]
public static class CreateTicketEndpoint
{
    public static async Task<ProblemDetails> ValidateAsync(CreateTicketCommand command, AppDbContext db, CancellationToken ct)
    {
        var categoryExit = await db.Categories.AnyAsync(c => c.Id == command.CategoryId, ct);
        if (!categoryExit)
        {
            return new ProblemDetails
            {
                Status = StatusCodes.Status404NotFound,
                Title = "Category not found",
                Detail = "Category not found"
            };
        }
        
        var slaExists = await db.SlaPolicies.AnyAsync(s => s.Priority == command.Priority, ct);
        if (!slaExists)
        {
            return new ProblemDetails 
            { 
                Status = StatusCodes.Status400BadRequest, 
                Title = "SLA Configuration Missing", 
                Detail = $"No SLA policy defined for {command.Priority} priority. Contact your administrator." 
            };
        }
        
        return WolverineContinue.NoProblems;
    }
    
    [Authorize]
    [WolverinePost("api/tickets")]
    [EndpointSummary("CreateTicket")]
    public static async Task<(Results<Ok<Guid>, ProblemHttpResult>, TicketCreatedEvent?)> Handle(
        CreateTicketCommand command,
        UserContext currentUser,
        AppDbContext db,
        CancellationToken ct)
    {

        var ticket = Ticket.Create(
            command.Title,
            command.Description,
            command.Priority,
            command.CategoryId,
            currentUser.UserId,
            currentUser.Name,
            currentUser.Email
            );
        
        var sla = await db.SlaPolicies
            .FirstOrDefaultAsync(s => s.Priority == command.Priority, ct);

        if (sla is not null)
        {
            var (response, resolution) = SlaDeadlineCalculator.CalculateDeadline(
                sla, DateTime.UtcNow);
            ticket.ApplySla(response, resolution);
        }

        db.Tickets.Add(ticket);
        await db.SaveChangesAsync(ct);
        
        var evt = new TicketCreatedEvent(
            ticket.Id,
            ticket.CategoryId,
            ticket.OrganizationId,
            ticket.Priority);
        
        return (TypedResults.Ok(ticket.Id), evt);
    }
}
