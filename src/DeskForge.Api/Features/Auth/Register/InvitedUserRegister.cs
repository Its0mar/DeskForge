using System.Diagnostics.CodeAnalysis;
using DeskForge.Api.Common.Dtos;
using DeskForge.Api.Features.Auth.Models;
using DeskForge.Api.Infrastructure.Auth.Token;
using DeskForge.Api.Infrastructure.Persistence;
using FluentValidation;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Wolverine.Attributes;
using Wolverine.Http;

namespace DeskForge.Api.Features.Auth.Register;

[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)]
public sealed record AcceptInviteCommand(string Token,string UserName,string FirstName, string LastName, string Password);

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
    [Transactional]
    [WolverinePost("api/auth/invites/accept")]
    [EndpointSummary("AcceptInvite")]
    public static async Task<Results<Ok<TokenResponse>, ProblemHttpResult>> Handle(
        AcceptInviteCommand command,
        AppDbContext db,
        UserManager<AppUser> userManager,
        ITokenProvider tokenProvider,
        CancellationToken ct)
    {
        var invite = await db.Invitations
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(i => i.InviteToken == command.Token, ct);

        if (invite is null || !invite.IsValid)
        {
            return InvitedUserRegisterErrors.InviteIsNullOrNotActive();
        }

        var appUser = new AppUser
        {
            UserName = command.UserName,
            Email = invite.Email,
            OrganizationId = invite.OrganizationId,
            FirstName = command.FirstName,
            LastName = command.LastName,
            Role = invite.Role
        };
        
        var identityResult = await userManager.CreateAsync(appUser, command.Password);
        
        if (!identityResult.Succeeded)
        {
            return InvitedUserRegisterErrors.IdentityError(identityResult);
        }
        
        invite.Accept(appUser.Id);
        await db.SaveChangesAsync(ct);
        
        var token = await tokenProvider.GenerateTokenAsync(appUser, ct);

        return TypedResults.Ok(token.Value);
    }
}

public static class InvitedUserRegisterErrors
{
    public static ProblemHttpResult InviteIsNullOrNotActive()
    {
        return TypedResults.Problem(
            title: "Invalid Invitation", 
            detail: "This invitation is either expired, revoked, or already used.", 
            statusCode: StatusCodes.Status400BadRequest);
    }

    public static ProblemHttpResult IdentityError(IdentityResult identityResult)
    {
        return TypedResults.Problem(
            title: "Registration Failed", 
            detail: identityResult.Errors.First().Description);
    }
}