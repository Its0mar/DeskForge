using DeskForge.Api.Common.Enums;

namespace DeskForge.Api.Infrastructure.Auth;

public record UserContext(
    Guid UserId,
    Guid OrganizationId,
    OrgRole Role);