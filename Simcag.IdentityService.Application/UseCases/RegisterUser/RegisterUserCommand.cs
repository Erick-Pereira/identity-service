namespace Simcag.IdentityService.Application.UseCases.Register;

using MediatR;

public sealed record RegisterCommand(
    Guid TenantId,
    string Email,
    string Password,
    string Name,
    string Role) : IRequest<RegisterCommandResult>;

public sealed record RegisterCommandResult(
    bool Success,
    string? Error,
    Guid? UserId,
    string? AccessToken,
    string? RefreshToken);
