namespace DeskForge.Api.Common.Entities;

public abstract class Entity
{
    public Guid Id { get; set; } = Guid.NewGuid();
}