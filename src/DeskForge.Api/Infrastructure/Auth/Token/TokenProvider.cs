using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using DeskForge.Api.Common.Dtos;
using DeskForge.Api.Common.Results;
using DeskForge.Api.Features.Auth.Models;
using DeskForge.Api.Infrastructure.Auth.Models;
using DeskForge.Api.Infrastructure.Persistence;
using Microsoft.CodeAnalysis.Options;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using JwtRegisteredClaimNames = Microsoft.IdentityModel.JsonWebTokens.JwtRegisteredClaimNames;

namespace DeskForge.Api.Infrastructure.Auth.Token;

public class TokenProvider(IConfiguration configuration, AppDbContext context, IOptions<JwtSettings> option) : ITokenProvider
{
    private readonly JwtSettings _jwtSettings = option.Value;
    public async Task<Result<TokenResponse>> GenerateTokenAsync(AppUser user, CancellationToken ct)
    {
        var key           = _jwtSettings.Secret;
        var issuer        = _jwtSettings.Issuer;
        var audience      = _jwtSettings.Audience;
        var expiryMinutes = DateTimeOffset.UtcNow.AddMinutes(_jwtSettings.TokenExpirationInMinutes);
        var expires = expiryMinutes.UtcDateTime;
        
        var claims = new List<Claim>
        {   
            new(JwtRegisteredClaimNames.Sub,   user.Id.ToString()),
            new(JwtRegisteredClaimNames.Email, user.Email!),
            new(JwtRegisteredClaimNames.Jti,   Guid.NewGuid().ToString()),
            new(ClaimTypes.Role,               user.Role.ToString()),
            new("org_id",                      user.OrganizationId.ToString()),
            new("name", $"{user.FirstName} {user.LastName}")
        };
        
        // Create access token
        var descriptor = new SecurityTokenDescriptor
        {
            Subject            = new ClaimsIdentity(claims),
            Expires            = expires,
            Issuer             = issuer,
            Audience           = audience,
            SigningCredentials = new SigningCredentials(
                new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key)),
                SecurityAlgorithms.HmacSha256Signature)
        };

        var handler       = new JwtSecurityTokenHandler();
        var accessToken   = handler.WriteToken(handler.CreateToken(descriptor));
        
        var now = DateTimeOffset.UtcNow;
        await context.RefreshTokens
            .Where(rt => rt.UserId == user.Id 
                         && (rt.IsRevoked || rt.ExpiresOnUtc < now))
            .ExecuteDeleteAsync(ct);
        
        var refreshResult = RefreshToken.Create(
            user.Id,
            DateTimeOffset.UtcNow.AddDays(7));

        if (refreshResult.IsError)
            return refreshResult.Errors!;

        await context.RefreshTokens.AddAsync(refreshResult.Value!, ct);
        await context.SaveChangesAsync(ct);

        return new TokenResponse(accessToken, refreshResult.Value!.Token, expires);

    }

    public ClaimsPrincipal? GetPrincipalFromExpiredToken(string token)
    {
        var validationParams = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey         = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(_jwtSettings.Secret)),
            ValidateIssuer   = true,
            ValidIssuer      = _jwtSettings.Issuer,
            ValidateAudience = true,
            ValidAudience    = _jwtSettings.Audience,
            ValidateLifetime = false
        };

        var handler   = new JwtSecurityTokenHandler();
        var principal = handler.ValidateToken(token, validationParams, out var securityToken);

        if (securityToken is not JwtSecurityToken jwt ||
            !jwt.Header.Alg.Equals(
                SecurityAlgorithms.HmacSha256,
                StringComparison.InvariantCultureIgnoreCase))
            return null;

        return principal;
    }
}