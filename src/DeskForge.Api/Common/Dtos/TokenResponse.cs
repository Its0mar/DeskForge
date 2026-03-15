namespace DeskForge.Api.Common.Dtos;

public sealed record TokenResponse(
    string AccessToken,
    string RefreshToken,
    DateTimeOffset ExpiresOnUtc);
