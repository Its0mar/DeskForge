using DeskForge.Api.Common.Enums;
using DeskForge.Api.Features.Sla;
using DeskForge.Api.Features.Tickets.Assignment;
using DeskForge.Api.Infrastructure.Auth.Models;
using DeskForge.Api.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Wolverine.Http;

namespace DeskForge.Api.Features.Tickets.Assignment;

public sealed record ReclassifyTicketRequest(
    Guid CategoryId,
    TicketPriority Priority);

[Tags("Ticket")]
public static class ReclassifyTicketEndpoint
{
    [Authorize]
    [WolverinePut("api/tickets/{ticketId}/classification")]
    [EndpointSummary("ReclassifyTicket")]
    public static async Task<
        Results<Ok, NotFound, ProblemHttpResult, ForbidHttpResult>>
        Handle(
            [FromRoute] Guid ticketId,
            ReclassifyTicketRequest request,
            AppDbContext db,
            TicketAssignmentEngine assignmentEngine,
            UserContext currentUser,
            CancellationToken ct)
    {
        var ticket = await db.Tickets
            .Include(t => t.Category)
            .FirstOrDefaultAsync(t => t.Id == ticketId, ct);

        if (ticket is null)
            return TypedResults.NotFound();

        // Only manager/owner
        if (currentUser.Role is OrgRole.Requester or OrgRole.Staff)
        {
            return TypedResults.Forbid();
        }

        if (ticket.Status == TicketStatus.Closed)
        {
            return TypedResults.Problem(
                title: "Ticket Closed",
                detail: "Closed tickets cannot be reclassified.",
                statusCode: StatusCodes.Status400BadRequest);
        }

        var category = await db.Categories
            .FirstOrDefaultAsync(c => c.Id == request.CategoryId, ct);

        if (category is null)
        {
            return TypedResults.Problem(
                title: "Category Not Found",
                detail: "Invalid category.",
                statusCode: StatusCodes.Status400BadRequest);
        }

        var sla = await db.SlaPolicies
            .FirstOrDefaultAsync(s => s.Priority == request.Priority, ct);

        if (sla is null)
        {
            return TypedResults.Problem(
                title: "SLA Missing",
                detail: "No SLA policy found for this priority.",
                statusCode: StatusCodes.Status400BadRequest);
        }

        // Update classification
        ticket.Reclassify(
            request.CategoryId,
            request.Priority);

        // Recalculate SLA
        var (response, resolution) =
            SlaDeadlineCalculator.CalculateDeadline(
                sla,
                DateTime.UtcNow);

        ticket.ApplySla(response, resolution);

        // Re-run assignment engine
        var staffId = await assignmentEngine.FindBestStaffAsync(
            db,
            ticket.OrganizationId,
            category.TargetTeamId,
            ct);

        if (staffId.HasValue)
        {
            ticket.AssignTo(staffId.Value);
        }

        await db.SaveChangesAsync(ct);

        return TypedResults.Ok();
    }
}