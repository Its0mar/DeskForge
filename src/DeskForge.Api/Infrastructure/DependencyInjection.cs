using System.Text;
using DeskForge.Api.Features.Auth.Models;
using DeskForge.Api.Infrastructure.Auth.Models;
using DeskForge.Api.Infrastructure.Auth.Token;
using DeskForge.Api.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

namespace DeskForge.Api.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services, 
        IConfiguration configuration)
    {
        // 1. Database Setup
        var connectionString = configuration.GetConnectionString("DefaultConnection");
        
        services.AddDbContext<AppDbContext>(options => 
            options.UseSqlite(connectionString));

        // 2. JWT Settings & Validation
        services.AddOptions<JwtSettings>()
            .Bind(configuration.GetSection(JwtSettings.SectionName))
            .ValidateOnStart();

        var jwtSettings = configuration.GetSection(JwtSettings.SectionName).Get<JwtSettings>();

        // 3. ASP.NET Core Identity
        services.AddIdentityCore<AppUser>(options =>
            {
                options.Password.RequireDigit = false;
                options.Password.RequiredLength = 6;
                options.Password.RequireNonAlphanumeric = false;
                options.Password.RequireUppercase = false;
            })
            .AddEntityFrameworkStores<AppDbContext>()
            .AddDefaultTokenProviders();

        // 4. Authentication & JWT Bearer
        services.AddAuthentication(options => {
            options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
        })
        .AddJwtBearer(options =>
        {
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                ValidIssuer = jwtSettings?.Issuer,
                ValidAudience = jwtSettings?.Audience,
                IssuerSigningKey = new SymmetricSecurityKey(
                    Encoding.UTF8.GetBytes(jwtSettings?.Secret ?? "DefaultSecretKeyForValidationFallback"))
            };
        });

        services.AddAuthorization();
        
        // 5. Custom services
        services.AddScoped<ITokenProvider, TokenProvider>();

        return services;
    }
}