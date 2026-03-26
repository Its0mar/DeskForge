using DeskForge.Api.Common.Entities;
using DeskForge.Api.Features.Auth.Models;

namespace DeskForge.Api.Features.Teams.Models;

public class TeamMembership : Entity
{
    public Guid TeamId { get; set; }
    public Guid UserId { get; set; }
    public Guid AddedByUserId { get; set; }
    public DateTime JoinedAtUtc { get; set; }
    public bool IsActive { get; set; }
    
    public AppUser User { get; private set; } = null!;
    public Team Team { get; private set; } = null!;
    
    private TeamMembership()
    { }
    
    internal static TeamMembership Create(
        Guid teamId,
        Guid userId,
        Guid addedByUserId)
    {
        return new TeamMembership
        {
            TeamId        = teamId,
            UserId        = userId,
            AddedByUserId = addedByUserId,
            JoinedAtUtc   = DateTime.UtcNow
        };
    }

    internal void Deactivate() => IsActive = false;
}