namespace DeskForge.Api.Features.Tickets.Events;

public sealed record TicketAssignedEvent(
    Guid TicketId,
    Guid StaffId);
