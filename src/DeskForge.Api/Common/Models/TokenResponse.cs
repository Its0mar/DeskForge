namespace DeskForge.Api.Common.Models;

public sealed record TokenResponse(
    string AccessToken,
    string RefreshToken,
    DateTimeOffset ExpiresOnUtc);
