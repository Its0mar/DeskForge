namespace DeskForge.Api.Features.Auth.Login;

public sealed record TokenResponse(
    string AccessToken,
    string RefreshToken,
    DateTimeOffset ExpiresOnUtc);
