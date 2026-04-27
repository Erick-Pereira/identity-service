namespace Simcag.IdentityService.Application.UseCases.GetProfile;

using MediatR;
using Microsoft.Extensions.Logging;
using Simcag.IdentityService.Application.DTOs;
using Simcag.IdentityService.Application.Interfaces;

public sealed class GetProfileQueryHandler : IRequestHandler<GetProfileQuery, UserProfileDto?>
{
    private readonly IUserRepository _users;
    private readonly ILogger<GetProfileQueryHandler> _logger;

    public GetProfileQueryHandler(IUserRepository users, ILogger<GetProfileQueryHandler> logger)
    {
        _users = users;
        _logger = logger;
    }

    public async Task<UserProfileDto?> Handle(GetProfileQuery request, CancellationToken ct)
    {
        var user = await _users.GetByIdAsync(request.UserId, request.TenantId, ct);
        if (user is null)
        {
            _logger.LogWarning("Perfil: utilizador {UserId} não encontrado no tenant {TenantId}", request.UserId, request.TenantId);
            return null;
        }

        return new UserProfileDto
        {
            Id = user.Id,
            TenantId = request.TenantId,
            Email = user.Email.Value,
            Name = user.Name,
            Role = user.Role.Value,
            CreatedAt = user.CreatedAt,
            IsActive = user.IsActive
        };
    }
}
