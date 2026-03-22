using DeskForge.Api.Common.Abstractions;

namespace DeskForge.Api.Common.Entities;

public abstract class AuditableEntity : Entity, IAuditableEntity
{
    public virtual Guid OrganizationId { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }
    public Guid? CreatedById { get; set; }
    
    public DateTimeOffset LastModifiedAtUtc { get; set; }
    public Guid? LastModifiedById { get; set; }

    public bool IsDeleted { get; set; } = false;
    public bool IsActive { get; set; } =  true;
}