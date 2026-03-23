using DeskForge.Api.Common.Entities;
using DeskForge.Api.Features.Teams.Models;

namespace DeskForge.Api.Features.Categories.Models;

public sealed class Category : AuditableEntity
{
    public required string Name { get; set; }
    public string? Description { get; set; }
    public Guid TargetTeamId { get; set; }
    public Team TargetTeam { get; init; } = null!;
}