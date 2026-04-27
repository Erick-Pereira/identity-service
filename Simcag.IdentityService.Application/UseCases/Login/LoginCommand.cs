namespace Simcag.IdentityService.Application.UseCases.Login;

using MediatR;

public sealed record LoginCommand(
    Guid TenantId,
    string Email,
    string Password) : IRequest<LoginCommandResult>;

public sealed record LoginCommandResult(
    bool Success,
    string? Error,
    string? AccessToken,
    string? RefreshToken,
    DateTime? ExpiresAt);
