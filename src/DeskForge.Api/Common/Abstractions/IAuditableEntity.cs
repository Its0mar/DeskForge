namespace DeskForge.Api.Common.Abstractions;

public interface IAuditableEntity
{
    Guid OrganizationId { get; set; }
    
    DateTimeOffset CreatedAtUtc { get; set; }
    Guid? CreatedById { get; set; }
    
    DateTimeOffset LastModifiedAtUtc { get; set; }
    Guid? LastModifiedById { get; set; }
    
    bool IsDeleted { get; set; } 
    bool IsActive { get; set; }
}