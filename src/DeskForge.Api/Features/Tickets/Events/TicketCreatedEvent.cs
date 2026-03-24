using DeskForge.Api.Common.Enums;

namespace DeskForge.Api.Features.Tickets.Events;

public record TicketCreatedEvent(
    Guid TicketId,
    Guid CategoryId,
    Guid OrgId,
    TicketPriority Priority);