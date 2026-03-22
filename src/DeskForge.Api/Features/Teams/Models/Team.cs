using DeskForge.Api.Common.Entities;
using DeskForge.Api.Common.Results;


namespace DeskForge.Api.Features.Teams.Models;

public class Team : AuditableEntity
{
    private readonly List<TeamMembership> _members = [];
    
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsActive { get; private set; } = true;
    public IReadOnlyCollection<TeamMembership> Members => _members.AsReadOnly();

    
    
    public Result<TeamMembership> AddMember(Guid userId, Guid addedByUserId)
    {
        if (_members.Any(m => m.Id == userId && m.IsActive))
            return Error.Conflict(
                "Team.Member.AlreadyExists",
                "User is already a member of this team.");

        var membership = TeamMembership.Create(Id, userId, addedByUserId);
        _members.Add(membership);

        return membership;
    }

    public Result<Success> RemoveMember(Guid userId)
    {
        var membership = _members
            .FirstOrDefault(m => m.UserId == userId && m.IsActive);

        if (membership is null)
            return Error.NotFound(
                "Team.Member.NotFound",
                "User is not a member of this team.");

        membership.Deactivate();
        return Result.Success;
    }

    public void Update(string name, string? description)
    {
        if (!string.IsNullOrWhiteSpace(name))
            Name = name.Trim();

        Description = description?.Trim();
    }

    public void Deactivate() => IsActive = false;
    public void Activate()   => IsActive = true;

}