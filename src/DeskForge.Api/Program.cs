using DeskForge.Api.Infrastructure;
using DeskForge.Api.Infrastructure.Auth;
using DeskForge.Api.Infrastructure.Exceptions;
using Scalar.AspNetCore;
using Wolverine;
using Wolverine.Http;
using Wolverine.Http.FluentValidation;
using FluentValidation;



var builder = WebApplication.CreateBuilder(args);

builder.Services.AddInfrastructure(builder.Configuration);

builder.Services.AddValidatorsFromAssemblyContaining<Program>();

builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();


builder.Host.UseWolverine(opts =>
{
    opts.Discovery.IncludeAssembly(typeof(Program).Assembly);
    // opts.UseFluentValidation(); 
});

builder.Services.AddWolverineHttp();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddOpenApi();



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
