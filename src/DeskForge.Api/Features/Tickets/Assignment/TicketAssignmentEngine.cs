using DeskForge.Api.Common.Enums;
using DeskForge.Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace DeskForge.Api.Features.Tickets.Assignment;

public class TicketAssignmentEngine
{
    public async Task<Guid?> FindBestStaffAsync(
        AppDbContext db,
        Guid orgId,
        Guid? targetTeamId,
        CancellationToken ct)
    {
        var now = DateTimeOffset.UtcNow;

        var query = db.Users
            .IgnoreQueryFilters()
            .Where(u =>
                u.OrganizationId == orgId &&
                u.Role == OrgRole.Staff &&
                u.IsActive &&
                !u.IsDeleted);

        if (targetTeamId is not null)
        {
            query = query.Where(u =>
                db.TeamMemberships.Any(tm =>
                    tm.UserId == u.Id &&
                    tm.TeamId == targetTeamId));
        }

        return await query
            .Select(u => new
            {
                u.Id,
                OpenTickets = db.Tickets.Count(t =>
                    t.AssignedToStaffId == u.Id &&
                    t.Status != TicketStatus.Closed &&
                    t.Status != TicketStatus.Resolved),

                // Null LastAssignedDate means never assigned → treat as very old (high bonus)
                HoursSinceLastAssign = u.LastAssignedDate == null
                    ? 9999.0
                    : (double)EF.Functions.DateDiffHour(u.LastAssignedDate, now)
            })
            .Select(u => new
            {
                u.Id,
                // Lower score = better candidate
                // Fewer open tickets and longer since last assigned = lower score
                Score = (u.OpenTickets * 10) - (u.HoursSinceLastAssign * 0.1)
            })
            .OrderBy(x => x.Score)
            .Select(x => x.Id)
            .FirstOrDefaultAsync(ct);
    }
}