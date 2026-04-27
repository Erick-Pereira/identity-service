namespace Simcag.IdentityService.Application.UseCases.RefreshToken;

using MediatR;
using Microsoft.Extensions.Logging;
using Simcag.IdentityService.Application.Interfaces;
using Simcag.IdentityService.Domain.Entities;
using Simcag.IdentityService.Domain.Results;

public sealed class RefreshTokenCommandHandler : IRequestHandler<RefreshTokenCommand, RefreshTokenResult>
{
    private readonly IRefreshTokenRepository _refreshTokens;
    private readonly IUserRepository _users;
    private readonly IJwtTokenService _jwt;
    private readonly ILogger<RefreshTokenCommandHandler> _logger;

    public RefreshTokenCommandHandler(
        IRefreshTokenRepository refreshTokens,
        IUserRepository users,
        IJwtTokenService jwt,
        ILogger<RefreshTokenCommandHandler> logger)
    {
        _refreshTokens = refreshTokens;
        _users = users;
        _jwt = jwt;
        _logger = logger;
    }

    public async Task<RefreshTokenResult> Handle(RefreshTokenCommand request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.RefreshToken))
            return new RefreshTokenResult(false, "Refresh token é obrigatório", null, null, null);

        var existing = await _refreshTokens.GetByTokenAsync(request.RefreshToken.Trim(), ct);
        if (existing is null || !existing.IsActive())
        {
            _logger.LogWarning("Refresh token inválido, expirado ou revogado");
            return new RefreshTokenResult(false, "Refresh token inválido ou expirado", null, null, null);
        }

        var tenantId = existing.TenantId.Value;
        var user = await _users.GetByIdAsync(existing.UserId, tenantId, ct);
        if (user is null || !user.IsActive)
        {
            _logger.LogWarning("Utilizador inativo ou inexistente no refresh: {UserId}", existing.UserId);
            return new RefreshTokenResult(false, "Utilizador não encontrado", null, null, null);
        }

        existing.Revoke();
        await _refreshTokens.UpdateAsync(existing, ct);

        var accessToken = await _jwt.GenerateAccessTokenAsync(
            user.Id, tenantId, user.Email.Value, user.Name, user.Role.Value, ct);
        var newRaw = await _jwt.GenerateRefreshTokenAsync(user.Id, tenantId, ct);

        var expires = DateTime.UtcNow.AddDays(_jwt.RefreshTokenExpirationDays);
        var newEntity = RefreshToken.Create(newRaw, user.Id, tenantId, expires);
        if (newEntity is Result<RefreshToken>.Failure f)
        {
            _logger.LogError("Falha ao criar novo refresh token: {Error}", f.Error);
            return new RefreshTokenResult(false, "Erro ao emitir novos tokens", null, null, null);
        }

        var newRt = newEntity.Match(
            v => v,
            e => throw new InvalidOperationException(e));
        await _refreshTokens.AddAsync(newRt, ct);

        return new RefreshTokenResult(
            true,
            null,
            accessToken,
            newRaw,
            DateTime.UtcNow.AddMinutes(_jwt.AccessTokenExpirationMinutes));
    }
}
