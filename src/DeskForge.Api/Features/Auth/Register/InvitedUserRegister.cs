using System.Diagnostics.CodeAnalysis;
using DeskForge.Api.Common.Models;
using DeskForge.Api.Features.Auth.Models;
using DeskForge.Api.Infrastructure.Auth.Token;
using DeskForge.Api.Infrastructure.Persistence;
using FluentValidation;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Wolverine.Attributes;
using Wolverine.Http;

namespace DeskForge.Api.Features.Auth.Register;

[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)]
public sealed record AcceptInviteCommand(string Token, string UserName, string FirstName, string LastName, string Password);

public sealed class AcceptInviteCommandValidator : AbstractValidator<AcceptInviteCommand>
{
    public AcceptInviteCommandValidator()
    {
        RuleFor(x => x.Token).NotEmpty();
        RuleFor(x => x.UserName).NotEmpty().MinimumLength(3).MaximumLength(30);
        RuleFor(x => x.FirstName).NotEmpty().MinimumLength(3).MaximumLength(20);
        RuleFor(x => x.LastName).NotEmpty().MinimumLength(3).MaximumLength(20);
        RuleFor(x => x.Password).NotEmpty().MinimumLength(6);
    }
}

[Tags("Invitations")]
public static class AcceptInviteEndpoint
{
    public static async Task<ProblemDetails> ValidateAsync(
        AcceptInviteCommand command,
        AppDbContext db,
        ILogger<AcceptInviteCommand> logger,
        CancellationToken ct)
    {
        var invite = await db.Invitations
            .IgnoreQueryFilters()
            .AsNoTracking()
            .FirstOrDefaultAsync(i => i.InviteToken == command.Token, ct);

        if (invite is null || !invite.IsValid)
        {
            logger.LogWarning("AcceptInvite: invalid or expired token attempted");
            return new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title  = "Invalid Invitation",
                Detail = "This invitation is either expired, revoked, or already used."
            };
        }

        return WolverineContinue.NoProblems;
    }

    [Transactional]
    [WolverinePost("api/auth/invites/accept")]
    [EndpointSummary("AcceptInvite")]
    public static async Task<Results<Ok<TokenResponse>, ProblemHttpResult>> Handle(
        AcceptInviteCommand command,
        AppDbContext db,
        UserManager<AppUser> userManager,
        ITokenProvider tokenProvider,
        ILogger<AcceptInviteCommand> logger,
        CancellationToken ct)
    {
        var invite = await db.Invitations
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(i => i.InviteToken == command.Token, ct);

        if (invite is null || !invite.IsValid)
            return InvitedUserRegisterErrors.InviteIsNullOrNotActive();

        var appUser = new AppUser
        {
            UserName       = command.UserName,
            Email          = invite.Email,
            OrganizationId = invite.OrganizationId,
            FirstName      = command.FirstName,
            LastName       = command.LastName,
            Role           = invite.Role
        };
        
        var identityResult = await userManager.CreateAsync(appUser, command.Password);
        if (!identityResult.Succeeded)
        {
            logger.LogWarning(
                "AcceptInvite registration failed for {Email}: {Error}",
                invite.Email, identityResult.Errors.First().Description);
            return InvitedUserRegisterErrors.IdentityError(identityResult);
        }
        
        invite.Accept(appUser.Id);
        await db.SaveChangesAsync(ct);
        
        var token = await tokenProvider.GenerateTokenAsync(appUser, ct);
        if (token.IsError)
        {
            logger.LogError(
                "Token generation failed after invite acceptance for User {UserId}: {Error}",
                appUser.Id, token.TopError.Description);
            return InvitedUserRegisterErrors.TokenGenerationError(token.TopError.Description);
        }

        logger.LogInformation(
            "Invite accepted: User {UserId} joined Org {OrgId} as {Role}",
            appUser.Id, appUser.OrganizationId, appUser.Role);

        return TypedResults.Ok(token.Value);
    }
}

public static class InvitedUserRegisterErrors
{
    public static ProblemHttpResult InviteIsNullOrNotActive() =>
        TypedResults.Problem(
            title: "Invalid Invitation", 
            detail: "This invitation is either expired, revoked, or already used.", 
            statusCode: StatusCodes.Status400BadRequest);

    public static ProblemHttpResult IdentityError(IdentityResult identityResult) =>
        TypedResults.Problem(
            title: "Registration Failed", 
            detail: identityResult.Errors.First().Description);

    public static ProblemHttpResult TokenGenerationError(string detail) =>
        TypedResults.Problem(
            title: "Auth Error",
            detail: detail,
            statusCode: StatusCodes.Status500InternalServerError);
}