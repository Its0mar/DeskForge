using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using DeskForge.Api.Common.Results;
using DeskForge.Api.Features.Auth.Login;
using DeskForge.Api.Features.Auth.Models;
using DeskForge.Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using JwtRegisteredClaimNames = Microsoft.IdentityModel.JsonWebTokens.JwtRegisteredClaimNames;

namespace DeskForge.Api.Infrastructure.Auth;

public class TokenProvider(IConfiguration configuration, AppDbContext context) : ITokenProvider
{
    public async Task<Result<TokenResponse>> GenerateTokenAsync(AppUser user, CancellationToken ct)
    {
        var jwtSettings   = configuration.GetSection("JwtSettings");
        var key           = jwtSettings["Secret"]!;
        var issuer        = jwtSettings["Issuer"]!;
        var audience      = jwtSettings["Audience"]!;
        var expiryMinutes = int.Parse(jwtSettings["TokenExpirationInMinutes"]!);
        var expires       = DateTimeOffset.UtcNow.AddMinutes(expiryMinutes);
        
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub,   user.Id.ToString()),
            new(JwtRegisteredClaimNames.Email, user.Email!),
            new(JwtRegisteredClaimNames.Jti,   Guid.NewGuid().ToString()),
            new("org_id",                      user.OrganizationId.ToString()),
            new("org_role",                    user.Role.ToString())
        };
        
        // Create access token
        var descriptor = new SecurityTokenDescriptor
        {
            Subject            = new ClaimsIdentity(claims),
            Expires            = expires.UtcDateTime,
            Issuer             = issuer,
            Audience           = audience,
            SigningCredentials = new SigningCredentials(
                new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key)),
                SecurityAlgorithms.HmacSha256Signature)
        };

        var handler       = new JwtSecurityTokenHandler();
        var accessToken   = handler.WriteToken(handler.CreateToken(descriptor));
        
        await context.Set<RefreshToken>()
            .Where(rt => rt.UserId == user.Id.ToString())
            .ExecuteDeleteAsync(ct);

        var refreshResult = RefreshToken.Create(
            user.Id.ToString(),
            DateTimeOffset.UtcNow.AddDays(7));

        if (refreshResult.IsError)
            return refreshResult.Errors!;

        context.Set<RefreshToken>().Add(refreshResult.Value!);
        await context.SaveChangesAsync(ct);

        return new TokenResponse(accessToken, refreshResult.Value!.Token, expires);

    }

    public ClaimsPrincipal? GetPrincipalFromExpiredToken(string token)
    {
        var validationParams = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey         = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(configuration["JwtSettings:Secret"]!)),
            ValidateIssuer   = true,
            ValidIssuer      = configuration["JwtSettings:Issuer"],
            ValidateAudience = true,
            ValidAudience    = configuration["JwtSettings:Audience"],
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