using System.Diagnostics.CodeAnalysis;
using DeskForge.Api.Common.Dtos;
using DeskForge.Api.Common.Enums;
using DeskForge.Api.Common.Results;
using DeskForge.Api.Features.Auth.Models;
using DeskForge.Api.Features.Organizations;
using DeskForge.Api.Features.Organizations.Models;
using DeskForge.Api.Infrastructure.Auth;
using DeskForge.Api.Infrastructure.Auth.Token;
using DeskForge.Api.Infrastructure.Persistence;
using FluentValidation;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Wolverine;
using Wolverine.Attributes;
using Wolverine.Http;

namespace DeskForge.Api.Features.Auth.Register;

[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)]
public record OrganizationRegisterCommand(string Name, string FirstName, string LastName, string TenantCode, string Email, string Password);

public sealed class OrganizationRegisterCommandValidator : AbstractValidator<OrganizationRegisterCommand>
{
    public  OrganizationRegisterCommandValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MinimumLength(3).MaximumLength(50);
        RuleFor(x => x.FirstName).NotEmpty().MinimumLength(3).MaximumLength(20);
        RuleFor(x => x.LastName).NotEmpty().MinimumLength(3).MaximumLength(20);
        RuleFor(x => x.Email).NotEmpty().EmailAddress().WithMessage("A valid email address is required.");
        RuleFor(x => x.Password).NotEmpty().MinimumLength(6);
        RuleFor(x => x.TenantCode)
            .NotEmpty().Matches("^[a-z0-9-]+$")
            .Must(x => !x.StartsWith("-") && !x.EndsWith("-"))
            .WithMessage("Tenant code cannot start or end with a hyphen.");;
    }
}

[Tags("Auth")]
public static class OrganizationRegisterEndpoint
{
    public static async Task<ProblemDetails> ValidateAsync(
        OrganizationRegisterCommand command,
        AppDbContext db,
        CancellationToken ct)
    {
        var emailExists = await db.Users.AsNoTracking().AnyAsync(u => u.Email == command.Email, ct);

        if (emailExists)
        {
            return new ProblemDetails
            {
                Status = StatusCodes.Status409Conflict,
                Title  = "Conflict",
                Detail = "Email already registered."
            };
        }
        
        var isTenantExist = await db.Organizations.AnyAsync(org => org.TenantCode == command.TenantCode, ct);
        if (isTenantExist)
        {
            return new ProblemDetails
            {
                Status = StatusCodes.Status409Conflict,
                Title = "Conflict",
                Detail = "This tenant code is already taken."
            };
        }

        return WolverineContinue.NoProblems;
    }
    
    [Transactional]
    [WolverinePost("api/auth/organization/register")]
    [EndpointSummary("Organization_Register")]
    public static async Task<Results<Ok<TokenResponse>, ProblemHttpResult>> Handle(
        OrganizationRegisterCommand command,
        IMessageBus bus,
        UserManager<AppUser> userManager,
        ITokenProvider tokenProvider,
        AppDbContext db,
        CancellationToken ct)
    {
        var organization = new Organization
        {
            Name = command.Name,
            TenantCode = command.TenantCode.ToLower().Trim()
        };

        db.Organizations.Add(organization);
        
        var user = new AppUser
        {
            UserName = command.Email,
            FirstName = command.FirstName,
            LastName = command.LastName,
            Email = command.Email,
            OrganizationId = organization.Id,
            Role = OrgRole.Owner
        };
        
        var identityResult = await userManager.CreateAsync(user, command.Password);
        if (!identityResult.Succeeded)
            return TypedResults.Problem(title: "User Creation Failed", detail: identityResult.Errors.First().Description);

        var token = await tokenProvider.GenerateTokenAsync(user, ct);

        if (token.IsError)
        {
            return TypedResults.Problem(
                title: "Auth Error", 
                detail: token.TopError.Description, 
                statusCode: 500);
        }
        
        return TypedResults.Ok(token.Value);
    }
    
}