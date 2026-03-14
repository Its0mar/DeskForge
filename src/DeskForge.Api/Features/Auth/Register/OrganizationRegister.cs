using System.Diagnostics.CodeAnalysis;
using DeskForge.Api.Common.Enums;
using DeskForge.Api.Common.Results;
using DeskForge.Api.Features.Auth.Login;
using DeskForge.Api.Features.Auth.Models;
using DeskForge.Api.Features.Organizations;
using DeskForge.Api.Infrastructure.Auth;
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
public record OrganizationRegisterCommand(string Name, string Password, string TenantCode, string Email);

public sealed class OrganizationRegisterCommandValidator : AbstractValidator<OrganizationRegisterCommand>
{
    public  OrganizationRegisterCommandValidator()
    {
        RuleFor(x => x.Password).NotEmpty().MinimumLength(6);
        RuleFor(x => x.Email).NotEmpty().EmailAddress().WithMessage("A valid email address is required.");
        
    }
}
//TODO : ADD FLUENT VALIDATION
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
            return new ProblemDetails
            {
                Status = StatusCodes.Status409Conflict,
                Title  = "Conflict",
                Detail = "Email already registered."
            };

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
        CancellationToken ct)
    {
        var orgResult = await bus.InvokeAsync<Result<Guid>>(new CreateOrganizationCommand(command.Name, command.TenantCode), ct);
        if (orgResult.IsError)
        {
            return TypedResults.Problem(
                title: orgResult.TopError.Code,
                detail: orgResult.TopError.Description,
                statusCode: StatusCodes.Status409Conflict);
        }
        
        var user = new AppUser
        {
            UserName = command.Email,
            Email = command.Email,
            OrganizationId = orgResult.Value,
            Role = OrgRole.Owner
        };
        
        var identityResult = await userManager.CreateAsync(user, command.Password);
        if (!identityResult.Succeeded)
            return TypedResults.Problem(title: "User Creation Failed", detail: identityResult.Errors.First().Description);

        var tokens = await tokenProvider.GenerateTokenAsync(user, ct);
        return TypedResults.Ok(tokens.Value);
    }
}