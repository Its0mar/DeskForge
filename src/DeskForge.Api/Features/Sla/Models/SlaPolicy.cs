using DeskForge.Api.Common.Entities;
using DeskForge.Api.Common.Enums;

namespace DeskForge.Api.Features.Sla.Models;

public class SlaPolicy : AuditableEntity
{ 
    public TicketPriority Priority { get; private set; }
    public int ResponseMinutes { get; private set; }
    public int ResolutionMinutes { get; private set; }

    private SlaPolicy() { }

    public static SlaPolicy Create(
        TicketPriority priority,
        int responseHours,
        int resolutionHours)
    {
        return new SlaPolicy
        {
            Priority         = priority,
            ResponseMinutes    = responseHours,
            ResolutionMinutes  = resolutionHours
        };
    }

    public void Update(int responseHours, int resolutionHours)
    {
        ResponseMinutes   = responseHours;
        ResolutionMinutes = resolutionHours;
    }
    
}