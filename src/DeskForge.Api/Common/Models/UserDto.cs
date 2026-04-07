namespace DeskForge.Api.Common.Models;

public record UserDto(
    string Id,
    string Username,
    string Email,
    string FirstName,
    string LastName,
    string Role,
    string TenantCode,
    string OrgId
);