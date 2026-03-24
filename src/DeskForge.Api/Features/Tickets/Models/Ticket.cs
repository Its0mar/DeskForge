using DeskForge.Api.Common.Entities;
using DeskForge.Api.Common.Enums;
using DeskForge.Api.Common.Results;

namespace DeskForge.Api.Features.Tickets.Models;

public class Ticket : AuditableEntity
{
    public string Title { get; private set; } = string.Empty;
    public string Description { get; private set; } = string.Empty;
    public TicketPriority Priority { get; private set; }
    public TicketStatus Status { get; private set; }
    public Guid CategoryId { get; private set; }
    public Guid? AssignedToStaffId { get; private set; }

    // SLA
    public DateTime? ResponseDeadline { get; private set; }
    public DateTime? ResolutionDeadline { get; private set; }
    public DateTime? FirstRespondedAt { get; private set; }
    public DateTime? ClosedAt { get; private set; }
    public DateTime LastActivityAt { get; private set; }

    public Guid SubmittedByUserId { get; private set; }
    public string SubmitterName { get; private set; } = string.Empty;
    public string SubmitterEmail { get; private set; } = string.Empty;
    
    private Ticket() { } // EF constructor

    public static Ticket Create(
        string title,
        string description,
        TicketPriority priority,
        Guid categoryId,
        Guid submittedByUserId,
        string submitterName,
        string submitterEmail)
    {
        return new Ticket
        {
            Title             = title,
            Description       = description,
            Priority          = priority,
            CategoryId        = categoryId,
            Status            = TicketStatus.New,
            LastActivityAt    = DateTime.UtcNow,
            SubmittedByUserId = submittedByUserId,
            SubmitterName     = submitterName,
            SubmitterEmail    = submitterEmail
        };
    }

    public Result<Updated> AssignTo(Guid staffId)
    {
        switch (Status)
        {
            case TicketStatus.Closed:
                return Error.Conflict(
                    "Ticket.Closed",
                    "Cannot assign a closed ticket.");
            
            case TicketStatus.Resolved:
                return Error.Conflict(
                    "Ticket.Resolved",
                    "Cannot assign a resolved ticket. Reopen it first.");
        }

        AssignedToStaffId = staffId;
        Status            = TicketStatus.InProgress;
        LastActivityAt    = DateTime.UtcNow;
        return Result.Updated;
    }

    public void ApplySla(DateTime responseDeadline, DateTime resolutionDeadline)
    {
        ResponseDeadline   = responseDeadline;
        ResolutionDeadline = resolutionDeadline;
    }

    public void MarkFirstResponse()
    {
        FirstRespondedAt ??= DateTime.UtcNow;
        LastActivityAt = DateTime.UtcNow;
    }
    
    public Result<Updated> Resolve()
    {
        if (Status != TicketStatus.InProgress)
            return Error.Conflict(
                "Ticket.InvalidTransition",
                $"Cannot resolve a ticket with status {Status}.");

        Status         = TicketStatus.Resolved;
        LastActivityAt = DateTime.UtcNow;
        return Result.Updated;
    }

    public Result<Updated> Close()
    {
        if (Status == TicketStatus.Closed)
            return Error.Conflict(
                "Ticket.AlreadyClosed",
                "Ticket is already closed.");

        Status         = TicketStatus.Closed;
        ClosedAt       = DateTime.UtcNow;
        LastActivityAt = DateTime.UtcNow;
        return Result.Updated;
    }

    public Result<Updated> Reopen()
    {
        if (Status != TicketStatus.Resolved)
            return Error.Conflict(
                "Ticket.InvalidTransition",
                "Only resolved tickets can be reopened.");

        Status         = TicketStatus.InProgress;
        LastActivityAt = DateTime.UtcNow;
        return Result.Updated;
    }

}