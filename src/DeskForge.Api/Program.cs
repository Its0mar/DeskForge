using System.Text.Json;
using DeskForge.Api.Infrastructure;
using DeskForge.Api.Infrastructure.Auth;
using DeskForge.Api.Infrastructure.Exceptions;
using FluentValidation;
using Scalar.AspNetCore;
using Serilog;
using Wolverine;
using Wolverine.Http;
using Wolverine.Http.FluentValidation;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowReactApp",
        policy =>
        {
            policy.WithOrigins("http://localhost:5173")
                .AllowAnyHeader()
                .AllowAnyMethod()
                .AllowCredentials();
        });
});

// 1. Serilog Setup
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .CreateLogger();

builder.Host.UseSerilog();

// 2. Service Registrations
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddValidatorsFromAssemblyContaining<Program>();
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();

builder.Host.UseWolverine(opts =>
{
    opts.Discovery.IncludeAssembly(typeof(Program).Assembly);
    // opts.Policies.AutoApplyTransactions();
});

builder.Services.AddWolverineHttp();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddOpenApi();

builder.Services.ConfigureHttpJsonOptions(opts =>
{
    opts.SerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
});

// 2. The Build
var app = builder.Build();

app.UseCors("AllowReactApp");

// 3. The Middleware Pipeline
app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

// app.UseMiddleware<UserContextMiddleware>();
app.MapWolverineEndpoints(opts => 
{

    opts.WarmUpRoutes = RouteWarmup.Eager;
    opts.UseFluentValidationProblemDetailMiddleware();
    opts.AddMiddleware(typeof(ClaimsPrincipalParserMiddleware));
});

app.Run();