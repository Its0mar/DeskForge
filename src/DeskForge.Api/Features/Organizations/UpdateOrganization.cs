using DeskForge.Api.Infrastructure.Auth.Models;
using DeskForge.Api.Infrastructure.Persistence;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Wolverine.Http;

namespace DeskForge.Api.Features.Organizations;

public sealed record UpdateOrganizationCommand(string Name);

public class UpdateOrganizationCommandValidator : AbstractValidator<UpdateOrganizationCommand>
{
    public UpdateOrganizationCommandValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MinimumLength(3).MaximumLength(50);
    }
}

[Tags("Organizations")]
public static class UpdateOrganizationEndpoint
{
    [Authorize(Policy = "OwnerOrManager")]
    [WolverinePut("api/organizations")]
    public static async Task<Ok<GetOrganizationQueryResponse>> Handle(
        UpdateOrganizationCommand command, 
        AppDbContext db, 
        UserContext currentUser, 
        CancellationToken ct)
    {
        var organization = await db.Organizations
            .FirstOrDefaultAsync(o => o.Id == currentUser.OrganizationId, ct);

        organization!.Update(command.Name);
        await db.SaveChangesAsync(ct);
        
        var orgResponse = new GetOrganizationQueryResponse(
            currentUser.OrganizationId, 
            command.Name, 
            organization.TenantCode
        );

        return TypedResults.Ok(orgResponse);
    }
}