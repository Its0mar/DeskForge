using System.Security.Claims;
using DeskForge.Api.Common.Results;
using DeskForge.Api.Features.Auth.Login;
using DeskForge.Api.Features.Auth.Models;

namespace DeskForge.Api.Infrastructure.Auth.Token;

public interface ITokenProvider
{
    Task<Result<TokenResponse>> GenerateTokenAsync(AppUser user, CancellationToken ct);
    ClaimsPrincipal? GetPrincipalFromExpiredToken(string token);
}