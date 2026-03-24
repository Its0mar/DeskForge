using DeskForge.Api.Features.Sla.Models;

namespace DeskForge.Api.Features.Sla;

public static class SlaDeadlineCalculator
{
    public static (DateTime Response, DateTime Resolution) CalculateDeadline(
        SlaPolicy policy,
        DateTime createdAt)
    {
        return (
            Response: createdAt.AddMinutes(policy.ResponseMinutes),
            Resolution: createdAt.AddMinutes(policy.ResolutionMinutes)
        );
    }
}