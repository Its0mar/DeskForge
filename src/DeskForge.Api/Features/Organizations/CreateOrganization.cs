using DeskForge.Api.Common.Controllers;
using DeskForge.Api.Common.Extensions;
using DeskForge.Api.Common.Results;
using DeskForge.Api.Features.Organizations.Models;
using DeskForge.Api.Infrastructure.Persistence;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Wolverine;

namespace DeskForge.Api.Features.Organizations;

public sealed record CreateOrganizationCommand(string Name, string TenantCode);

public sealed class CreateOrganizationCommandValidator : AbstractValidator<CreateOrganizationCommand>
{
    public CreateOrganizationCommandValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Name is required")
            .MinimumLength(3).WithMessage("Name must be at least 3 characters long")
            .MaximumLength(100).WithMessage("Name must be less than 100 characters long");
        
        RuleFor(x => x.TenantCode)
            .NotEmpty().WithMessage("Tenant code must be provided")
            .MinimumLength(3).WithMessage("Tenant Code must be at least 3 characters long")
            .MaximumLength(100).WithMessage("Tenant Code must be less than 100 characters long")
            .Matches("^[a-z0-9-]+$").WithMessage("Tenant code can only contain lowercase letters, numbers, and hyphens.");
    }
}

public sealed class CreateOrganizationCommandHandler(
    AppDbContext context,
    IValidator<CreateOrganizationCommand> validator,
    ILogger<CreateOrganizationCommandHandler> logger)
{
    public async Task<Result<Guid>> Handle(CreateOrganizationCommand command, CancellationToken ct)
    {
        var validationResult = await validator.ValidateAsync(command, ct);
        if (!validationResult.IsValid)
        {
            return validationResult.ToErrors();
        }
        
        var isTenantExist = await context.Organizations.AnyAsync(org => org.TenantCode == command.TenantCode, ct);

        if (isTenantExist)
        {
            return Error.Failure("Org.Duplicate", "This tenant code is already taken.");
        }

        var organization = new Organization
        {
            Name = command.Name,
            TenantCode = command.TenantCode.ToLower().Trim()
        };

        context.Organizations.Add(organization);
        await context.SaveChangesAsync(ct);

        return organization.Id;
    }
}

public class CreateOrganizationController(IMessageBus bus) : ApiController 
{
    [HttpPost("api/organizations/create")]
    public async Task<ActionResult> Create([FromBody]CreateOrganizationCommand command, CancellationToken ct)
    {
        var result = await bus.InvokeAsync<Result<Guid>>(command, ct);
        return result.Match(
            response => Ok(result),
            Problem);
    }
}