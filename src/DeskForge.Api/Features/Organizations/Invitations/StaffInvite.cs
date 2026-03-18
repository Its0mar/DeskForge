using System.Diagnostics.CodeAnalysis;
using DeskForge.Api.Common.Enums;
using DeskForge.Api.Features.Auth.Models;
using DeskForge.Api.Features.Organizations.Models;
using DeskForge.Api.Infrastructure.Auth.Models;
using DeskForge.Api.Infrastructure.Persistence;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Wolverine.Http;

namespace DeskForge.Api.Features.Organizations.Invitations;

[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)]
public sealed record SendInviteCommand(string Email, OrgRole Role);

public sealed class SendInviteCommandValidator : AbstractValidator<SendInviteCommand>
{
    public SendInviteCommandValidator()
    {
        RuleFor(x => x.Email).NotNull().NotEmpty().EmailAddress().WithMessage("Please specify a valid email address");
        RuleFor(x => x.Role).NotNull().NotEmpty().WithMessage("Please specify a role");
        RuleFor(x => x.Role).NotEqual(OrgRole.Owner).WithMessage("Cannot invite another Owner.");
        RuleFor(x => x.Role).NotEqual(OrgRole.Customer).WithMessage("Cannot invite a customer.");
    }
}

[Tags("Invitations")]
public class SendInviteEndpoint
{
    public async Task<ProblemDetails> ValidateAsync(SendInviteCommand command, UserContext inviter, AppDbContext db, UserManager<AppUser> userManager, CancellationToken ct)
    {
        var emailTaken = await db.Users.AnyAsync(u => u.Email == command.Email, ct);
        if (emailTaken)
        {
            return new ProblemDetails
            {
                Status = StatusCodes.Status409Conflict,
                Title = "Email already registered",
                Detail = "User with this email already registered"
            };
        }
        
        if (inviter.Role == OrgRole.Manager && command.Role == OrgRole.Manager)
        {
            return new ProblemDetails
            {
                Status = StatusCodes.Status403Forbidden,
                Title = "Forbidden",
                Detail = "Managers are only permitted to invite Staff members."
            };
        }

        return WolverineContinue.NoProblems;
    }
    
    [Authorize(Roles = "Owner, Manager")]
    [WolverinePost("api/organizations/invite-employee")]
    public async Task<Results<Ok<string>, ProblemHttpResult>> Handle(SendInviteCommand command, UserContext currentUser, AppDbContext db,
        CancellationToken ct)
    {
        var invitation = OrgInvite.Create(command.Email, command.Role, currentUser.OrganizationId);
        
        await db.Invitations.AddAsync(invitation, ct);
        await db.SaveChangesAsync(ct);

        return TypedResults.Ok(invitation.InviteToken);
    }
}