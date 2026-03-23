using System.Diagnostics.CodeAnalysis;
using DeskForge.Api.Features.Categories.Models;
using DeskForge.Api.Infrastructure.Persistence;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Wolverine.Http;

namespace DeskForge.Api.Features.Categories;

[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)]
public sealed record CreateCategoryCommand(string Name, string? Description, Guid TargetTeamId);

public sealed class CreateCategoryCommandValidator : AbstractValidator<CreateCategoryCommand>
{
    public  CreateCategoryCommandValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Name is required")
            .Length(3, 30).WithMessage("Category name must be between 3 and 30 characters long.");
        
        RuleFor(x => x.Description)
            .MinimumLength(15).MaximumLength(300)
            .WithMessage("Category description must be between 15 and 300 characters long.")
            .When(x => x.Description is not null);
        
        RuleFor(x => x.TargetTeamId).NotEmpty().WithMessage("Target team id is required");
    }
}

[Tags("Category")]
public static class CreateCategoryEndpoint
{
    public static async Task<ProblemDetails> ValidateAsync(
        CreateCategoryCommand command,
        AppDbContext db,
        CancellationToken ct)
    {
        var teamExist = await db.Teams.AnyAsync(t => t.Id == command.TargetTeamId, ct);
        if (!teamExist)
        {
            return new ProblemDetails
            {
                Status = StatusCodes.Status404NotFound,
                Title = "Target team not found",
                Detail = "Target team not found"
            };
        }

        var nameExist = await db.Categories
            .AnyAsync(c => c.Name == command.Name && c.TargetTeamId == command.TargetTeamId, ct);
        if (nameExist)
        {
            return new ProblemDetails
            {
                Status = StatusCodes.Status409Conflict,
                Title = "Name already taken",
                Detail = "Name already taken"
            };
        }

        return WolverineContinue.NoProblems;
    }

    [Authorize(Policy = "OwnerOrManager")]
    [WolverinePost("api/categories")]
    [EndpointSummary("CreateCategory")]
    public static async Task<Ok<Guid>> Handle(CreateCategoryCommand command, AppDbContext db, CancellationToken ct)
    {
        var category = new Category
        {
            Name = command.Name,
            Description = command.Description,
            TargetTeamId = command.TargetTeamId
        };
        
        db.Categories.Add(category);
        await db.SaveChangesAsync(ct);
        
        return TypedResults.Ok(category.Id);
    }
    
}