using DeskForge.Api.Infrastructure.Exceptions;
using DeskForge.Api.Infrastructure.Persistence;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Scalar.AspNetCore;
using Wolverine;


var builder = WebApplication.CreateBuilder(args);
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");


builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddLogging();
builder.Services.AddDbContext<AppDbContext>(options => 
    options.UseSqlite(connectionString)
    );

builder.Services.AddValidatorsFromAssembly(typeof(Program).Assembly);

builder.Host.UseWolverine(opt =>
{
    opt.Discovery.IncludeAssembly(typeof(Program).Assembly);
});

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

builder.Services.AddOpenApi();


var app = builder.Build();

app.UseExceptionHandler();
// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

app.UseHttpsRedirection();
app.MapControllers();

app.Run();
