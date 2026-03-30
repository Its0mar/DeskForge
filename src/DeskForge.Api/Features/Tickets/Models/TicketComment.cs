using DeskForge.Api.Common.Entities;

namespace DeskForge.Api.Features.Tickets.Models;

public class TicketComment : AuditableEntity
{
    public required string Content { get; set; }
    public bool IsInternal { get; set; }
    
    public Guid TicketId { get; set; }
    public Guid AuthorId { get; set; }
    
    private TicketComment() {}

    public static TicketComment Create(string content, bool isInternal, Guid ticketId, Guid authorId)
    {
        return new TicketComment
        {
            Content = content,
            IsInternal = isInternal,
            TicketId = ticketId,
            AuthorId = authorId
        };
    }
}