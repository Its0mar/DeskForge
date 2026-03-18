namespace DeskForge.Api.Common.Entities;

public abstract class AuditableEntity : Entity
{
    public DateTimeOffset CreatedAtUtc { get; set; }
    public Guid? CreatedById { get; set; }
    
    public DateTimeOffset LastModifiedAtUtc { get; set; }
    public Guid? LastModifiedById { get; set; }
    
}