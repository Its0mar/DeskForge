using System.Security.Claims;
using System.Text;
using DeskForge.Api.Common.Enums;
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
        // 1. Persistence
        services.AddDbContext<AppDbContext>(options => 
            options.UseSqlite(configuration.GetConnectionString("DefaultConnection")));

        // 2. Strongly Typed Configurations
        services.Configure<JwtSettings>(configuration.GetSection(JwtSettings.SectionName));
        // services.Configure<InviteSettings>(configuration.GetSection(InviteSettings.SectionName));

        // 3. Identity
        services.AddIdentityCore<AppUser>(options =>
            {
                options.Password.RequireDigit = false;
                options.Password.RequiredLength = 6;
                options.Password.RequireNonAlphanumeric = false;
                options.Password.RequireUppercase = false;
            })
            .AddEntityFrameworkStores<AppDbContext>()
            .AddDefaultTokenProviders();

        // 4. Authentication
        AddAuth(services, configuration);

        // 5. Infrastructure Services
        services.AddScoped<ITokenProvider, TokenProvider>();
        services.AddScoped<UserContext>();
        
        services.AddHttpContextAccessor();

        return services;
    }

    private static void AddAuth(IServiceCollection services, IConfiguration configuration)
    {
        var jwtSettings = configuration.GetSection(JwtSettings.SectionName).Get<JwtSettings>();

        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
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
                        Encoding.UTF8.GetBytes(jwtSettings?.Secret ?? "YourSuperSecretKeyGoesHere"))
                };
            });

        services.AddAuthorization(opts =>
        {
            opts.AddPolicy("OwnerOrManager", policy =>
                policy.RequireClaim(ClaimTypes.Role,
                    nameof(OrgRole.Owner),
                    nameof(OrgRole.Manager)));

            opts.AddPolicy("OwnerOnly", policy =>
                policy.RequireClaim("org_role",
                    nameof(OrgRole.Owner)));
        });
    }
}