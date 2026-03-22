using DeskForge.Api.Infrastructure;
using DeskForge.Api.Infrastructure.Auth;
using DeskForge.Api.Infrastructure.Exceptions;
using FluentValidation;
using Scalar.AspNetCore;
using Wolverine;
using Wolverine.Http;
using Wolverine.Http.FluentValidation;

var builder = WebApplication.CreateBuilder(args);

// 1. Service Registrations
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddValidatorsFromAssemblyContaining<Program>();
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();

builder.Host.UseWolverine(opts =>
{
    opts.Discovery.IncludeAssembly(typeof(Program).Assembly);
});

builder.Services.AddWolverineHttp();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddOpenApi();

// 2. The Build
var app = builder.Build();

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