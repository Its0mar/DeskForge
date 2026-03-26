using DeskForge.Api.Common.Enums;
using DeskForge.Api.Features.Tickets.Events;
using DeskForge.Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace DeskForge.Api.Features.Tickets;

public class TicketCreatedHandler
{
    public async Task Handle(
        TicketCreatedEvent evt,
        IDbContextFactory<AppDbContext> dbFactory,
        CancellationToken ct)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);

        var ticket = await db.Tickets
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(t => t.Id == evt.TicketId, ct);

        if (ticket is null) return;

        var category = await db.Categories
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(c => c.Id    == evt.CategoryId
                                   && c.OrganizationId == evt.OrgId, ct);

        var staffId = category?.TargetTeamId is not null
            ? await FindStaffInTeam(db, category.TargetTeamId, evt.OrgId, ct)
            : await FindLeastBusyStaff(db, evt.OrgId, ct);

    
        if (staffId.HasValue)
        {
            ticket.AssignTo(staffId.Value);

            await db.Users
                .IgnoreQueryFilters()
                .Where(u => u.Id == staffId.Value)
                .ExecuteUpdateAsync(s => 
                    s.SetProperty(x => x.LastAssignedDate, DateTimeOffset.UtcNow)
                    , ct);


            await db.SaveChangesAsync(ct);
        }
    }

    private static async Task<Guid?> FindStaffInTeam(
        AppDbContext db,
        Guid teamId,
        Guid orgId,
        CancellationToken ct)
    {
        return (await db.TeamMemberships
            .IgnoreQueryFilters()
            .Where(tm => tm.TeamId == teamId)  
            .Select(tm => tm.User)
            .Where(u => u.OrganizationId == orgId
                     && u.Role           == OrgRole.Staff
                     && u.IsActive
                     && !u.IsDeleted)
            .Select(u => new
            {
                u.Id,
                u.LastAssignedDate,
                OpenTickets = db.Tickets
                    .IgnoreQueryFilters()
                    .Count(t => t.AssignedToStaffId == u.Id
                             && t.Status != TicketStatus.Closed
                             && t.Status != TicketStatus.Resolved)
            })
            .OrderBy(u => u.OpenTickets)
            .ThenBy(u => u.LastAssignedDate)
            .FirstOrDefaultAsync(ct))?.Id;
    }
    
    private static async Task<Guid?> FindLeastBusyStaff(
        AppDbContext db,
        Guid orgId,
        CancellationToken ct)
    {
        return (await db.Users
            .IgnoreQueryFilters()
            .Where(u => u.OrganizationId == orgId
                        && u.Role           == OrgRole.Staff
                        && u.IsActive
                        && !u.IsDeleted)
            .Select(u => new
            {
                u.Id,
                u.LastAssignedDate,
                OpenTickets = db.Tickets
                    .IgnoreQueryFilters()
                    .Count(t => t.AssignedToStaffId == u.Id
                                && t.Status != TicketStatus.Closed
                                && t.Status != TicketStatus.Resolved)
            })
            .OrderBy(u => u.OpenTickets)
            .ThenBy(u => u.LastAssignedDate)
            .FirstOrDefaultAsync(ct))?.Id;
    }
}