using DeskForge.Api.Common.Enums;
using DeskForge.Api.Features.Tickets.Events;
using DeskForge.Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace DeskForge.Api.Features.Tickets;

public class TicketCreatedHandler(
    IDbContextFactory<AppDbContext> dbFactory,
    TicketAssignmentEngine engine)
{
    public async Task Handle(
        TicketCreatedEvent evt,
        CancellationToken ct)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);

        var ticket = await db.Tickets
            .IgnoreQueryFilters()
            .Include(t => t.Category)
            .FirstOrDefaultAsync(t => t.Id == evt.TicketId, ct);

        if (ticket is null || !ticket.IsActive)
            return;

        var teamId = ticket.Category.TargetTeamId;

        var staffId = await engine.FindBestStaffAsync(
            db,
            evt.OrgId,
            teamId,
            ct);

        if (staffId is null || staffId == Guid.Empty)
            return;

        ticket.AssignTo(staffId.Value);

        // Stamp the staff member so the scoring engine knows when they were last assigned
        await db.Users
            .IgnoreQueryFilters()
            .Where(u => u.Id == staffId.Value)
            .ExecuteUpdateAsync(s => s.SetProperty(u => u.LastAssignedDate, DateTimeOffset.UtcNow), ct);

        await db.SaveChangesAsync(ct);
    }
}