using DeskForge.Api.Common.Abstractions;
using DeskForge.Api.Common.Entities;

namespace DeskForge.Api.Features.Organizations.Models;

public class Organization : AuditableEntity
{
    public string Name { get; init; } =  string.Empty;
    public string TenantCode { get; init; } = string.Empty;

    public override Guid OrganizationId 
    { 
        get => Id; 
        set => Id = value; 
    }
    
}