namespace Simcag.IdentityService.Application.UseCases.Logout;

using MediatR;

public sealed record LogoutCommand(string? RefreshToken) : IRequest;
