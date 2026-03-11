namespace DeskForge.Api.Common.Entities;

public abstract class AuditableEntity : Entity
{
    public DateTimeOffset CreatedAtUtc { get; set; }
    public string? CreatedBy { get; set; }
    public DateTimeOffset LastModifiedAtUtc { get; set; }
    public string? LastModifiedBy { get; set; }
}