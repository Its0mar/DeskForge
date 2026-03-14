using System.Text;
using DeskForge.Api.Features.Auth.Models;
using DeskForge.Api.Infrastructure.Auth;
using DeskForge.Api.Infrastructure.Exceptions;
using DeskForge.Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Scalar.AspNetCore;
using Wolverine;
using Wolverine.Http;
using Wolverine.Http.FluentValidation;
using FluentValidation;
using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;


var builder = WebApplication.CreateBuilder(args);
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");


builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddDbContext<AppDbContext>(options => 
    options.UseSqlite(connectionString));

builder.Services.AddValidatorsFromAssemblyContaining<Program>();

builder.Services.AddIdentityCore<AppUser>(options =>
    {
        options.Password.RequireDigit = false;
        options.Password.RequiredLength = 6;
        options.Password.RequireNonAlphanumeric = false;
        options.Password.RequireUppercase = false;
    })
    .AddEntityFrameworkStores<AppDbContext>()
    .AddDefaultTokenProviders();

builder.Services.AddAuthentication().AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = builder.Configuration["JwtSettings:Issuer"],
        ValidAudience = builder.Configuration["JwtSettings:Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(builder.Configuration["JwtSettings:Secret"]!))
    };
});
builder.Services.AddAuthorization();

builder.Host.UseWolverine(opts =>
{
    opts.Discovery.IncludeAssembly(typeof(Program).Assembly);
    // opts.UseFluentValidation(); 
});

builder.Services.AddWolverineHttp();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddOpenApi();

builder.Services.AddScoped<ITokenProvider, TokenProvider>();

var app = builder.Build();

app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

app.UseHttpsRedirection();

app.MapWolverineEndpoints(opts => 
{
    opts.WarmUpRoutes = RouteWarmup.Eager;
    
    // Automatically transforms FluentValidation failures into ProblemDetails (400)
    opts.UseFluentValidationProblemDetailMiddleware();
    opts.AddMiddleware(typeof(UserContextMiddleware));
});

app.Run();
