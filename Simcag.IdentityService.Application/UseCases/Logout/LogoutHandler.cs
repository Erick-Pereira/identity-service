namespace Simcag.IdentityService.Application.UseCases.Logout;

using MediatR;
using Microsoft.Extensions.Logging;
using Simcag.IdentityService.Application.Interfaces;

public sealed class LogoutCommandHandler : IRequestHandler<LogoutCommand>
{
    private readonly IRefreshTokenRepository _refreshTokens;
    private readonly ILogger<LogoutCommandHandler> _logger;

    public LogoutCommandHandler(
        IRefreshTokenRepository refreshTokens,
        ILogger<LogoutCommandHandler> logger)
    {
        _refreshTokens = refreshTokens;
        _logger = logger;
    }

    public async Task Handle(LogoutCommand request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.RefreshToken))
        {
            _logger.LogDebug("Logout sem refresh no body — só descarte no cliente");
            return;
        }

        var token = await _refreshTokens.GetByTokenAsync(request.RefreshToken.Trim(), ct);
        if (token is null)
        {
            _logger.LogDebug("Logout: refresh não encontrado (não expõe se existiu)");
            return;
        }

        if (!token.IsActive())
        {
            _logger.LogDebug("Logout: refresh já inativo");
            return;
        }

        token.Revoke();
        await _refreshTokens.UpdateAsync(token, ct);
        _logger.LogInformation("Refresh token revogado no logout: {TokenId}", token.Id);
    }
}
