using DeskForge.Api.Common.Entities;

namespace DeskForge.Api.Features.Organizations.Models;

public class Organization : AuditableEntity
{
    public string Name { get; init; } =  string.Empty;
    public string TenantCode { get; init; } = string.Empty;
    public bool IsActive { get; init; } = true;
}