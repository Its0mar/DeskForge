using System.Runtime.CompilerServices;
using DeskForge.Api.Common.Entities;
using DeskForge.Api.Common.Results;

namespace DeskForge.Api.Features.Auth.Models;

public class RefreshToken
{
    public Guid Id { get; private set; } = Guid.NewGuid();
    public string Token { get; private set; }
    public string UserId { get; private set; }
    public DateTimeOffset ExpiresOnUtc { get; private set; }
    public bool IsExpired => DateTimeOffset.UtcNow >= ExpiresOnUtc;
    public bool IsRevoked { get; private set; }
    public bool IsValid => !IsExpired && !IsRevoked;
    
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.
    private RefreshToken()
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.
    { }

    private RefreshToken(string token, string userId, DateTimeOffset expiresOnUtc)
    {
        Token = token;
        UserId = userId;
        ExpiresOnUtc = expiresOnUtc;
    }

    public static Result<RefreshToken> Create(string userId, DateTimeOffset expiresOnUtc)
    {
        if (expiresOnUtc <= DateTimeOffset.UtcNow)
            return Error.Validation("RefreshToken.Expiry.Invalid", "Expiry must be in the future.");

        var token = Convert.ToBase64String(System.Security.Cryptography.RandomNumberGenerator.GetBytes(64));
        return new RefreshToken(token, userId, expiresOnUtc);
    }

    public void Revoke() => IsRevoked = true;
    
}