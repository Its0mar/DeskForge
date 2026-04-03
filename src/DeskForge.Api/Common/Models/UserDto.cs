namespace DeskForge.Api.Common.Models;

public record UserDto(
    string Id,
    string Email,
    string FirstName,
    string LastName,
    string Role
);