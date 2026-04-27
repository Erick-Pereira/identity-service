namespace Simcag.IdentityService.Application.UseCases.RefreshToken;

using MediatR;

public sealed record RefreshTokenCommand(string RefreshToken) : IRequest<RefreshTokenResult>;

public sealed record RefreshTokenResult(
    bool Success,
    string? Error,
    string? AccessToken,
    string? NewRefreshToken,
    DateTime? ExpiresAt);
