namespace Simcag.IdentityService.Infrastructure.Repositories;

using Microsoft.EntityFrameworkCore;
using Simcag.IdentityService.Application.Interfaces;
using Simcag.IdentityService.Domain.Entities;
using Simcag.IdentityService.Infrastructure.Persistence.DbContext;
using Microsoft.Extensions.Logging;

public sealed class RefreshTokenRepository : IRefreshTokenRepository
{
    private readonly IdentityServiceDbContext _dbContext;
    private readonly ILogger<RefreshTokenRepository> _logger;

    public RefreshTokenRepository(IdentityServiceDbContext dbContext, ILogger<RefreshTokenRepository> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<RefreshToken?> GetByTokenAsync(string token, CancellationToken ct)
    {
        // Tracking: necessário para Revoke + Update após refresh
        return await _dbContext.RefreshTokens
            .FirstOrDefaultAsync(rt => rt.Token == token, ct);
    }

    public async Task AddAsync(RefreshToken refreshToken, CancellationToken ct)
    {
        await _dbContext.RefreshTokens.AddAsync(refreshToken, ct);
        await _dbContext.SaveChangesAsync(ct);
        _logger.LogInformation("Refresh token adicionado para usuário: {UserId}", refreshToken.UserId);
    }

    public async Task UpdateAsync(RefreshToken refreshToken, CancellationToken ct)
    {
        _dbContext.RefreshTokens.Update(refreshToken);
        await _dbContext.SaveChangesAsync(ct);
        _logger.LogInformation("Refresh token atualizado: {TokenId}", refreshToken.Id);
    }

    public async Task RevokeAllForUserAsync(Guid userId, Guid tenantId, CancellationToken ct)
    {
        var activeTokens = await _dbContext.RefreshTokens
            .Where(rt => rt.UserId == userId && rt.TenantId.Value == tenantId && !rt.IsRevoked && rt.ExpiresAt > DateTime.UtcNow)
            .ToListAsync(ct);

        foreach (var token in activeTokens)
        {
            token.Revoke();
        }

        await _dbContext.SaveChangesAsync(ct);
        _logger.LogInformation("Revogados {Count} tokens para usuário: {UserId}", activeTokens.Count, userId);
    }

    public async Task<IEnumerable<RefreshToken>> GetActiveTokensForUserAsync(Guid userId, Guid tenantId, CancellationToken ct)
    {
        return await _dbContext.RefreshTokens
            .AsNoTracking()
            .Where(rt => rt.UserId == userId && rt.TenantId.Value == tenantId && !rt.IsRevoked && rt.ExpiresAt > DateTime.UtcNow)
            .ToListAsync(ct);
    }
}