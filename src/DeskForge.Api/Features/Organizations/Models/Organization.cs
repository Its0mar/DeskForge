using DeskForge.Api.Common.Entities;

namespace DeskForge.Api.Features.Organizations.Models;

public class Organization : AuditableEntity
{
    public string Name { get; set; } =  string.Empty;
    public string TenantCode { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
}