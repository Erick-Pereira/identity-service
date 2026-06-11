namespace Simcag.IdentityService.Application.Interfaces;

using Simcag.IdentityService.Domain.Entities;

public interface IUserRepository
{
    Task<User?> GetByIdAsync(Guid id, Guid tenantId, CancellationToken ct);
    Task<User?> GetByEmailAndTenantAsync(string email, Guid tenantId, CancellationToken ct);
    Task<IReadOnlyList<User>> GetActiveByTenantAndRolesAsync(
        Guid tenantId,
        IReadOnlyCollection<string> roles,
        CancellationToken ct);
    Task AddAsync(User user, CancellationToken ct);
    Task UpdateAsync(User user, CancellationToken ct);
    Task<bool> EmailExistsAsync(string email, Guid tenantId, CancellationToken ct);
    Task<bool> ExistsByEmailInAnyTenantAsync(string email, CancellationToken ct);
}

public interface IRefreshTokenRepository
{
    Task<RefreshToken?> GetByTokenAsync(string token, CancellationToken ct);
    Task AddAsync(RefreshToken refreshToken, CancellationToken ct);
    Task UpdateAsync(RefreshToken refreshToken, CancellationToken ct);
    Task RevokeAllForUserAsync(Guid userId, Guid tenantId, CancellationToken ct);
    Task<IEnumerable<RefreshToken>> GetActiveTokensForUserAsync(Guid userId, Guid tenantId, CancellationToken ct);
}