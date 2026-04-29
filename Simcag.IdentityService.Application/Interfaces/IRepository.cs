namespace Simcag.IdentityService.Application.Interfaces;

using Simcag.IdentityService.Domain.Entities;

public interface IUserRepository
{
    Task<User?> GetByIdAsync(Guid id, Guid tenantId, CancellationToken ct);
    Task<User?> GetByEmailAndTenantAsync(string email, Guid tenantId, CancellationToken ct);
    Task AddAsync(User user, CancellationToken ct);
    Task UpdateAsync(User user, CancellationToken ct);
    Task<bool> EmailExistsAsync(string email, Guid tenantId, CancellationToken ct);
}

public interface IRefreshTokenRepository
{
    Task<RefreshToken?> GetByTokenAsync(string token, CancellationToken ct);
    Task AddAsync(RefreshToken refreshToken, CancellationToken ct);
    Task UpdateAsync(RefreshToken refreshToken, CancellationToken ct);
    Task RevokeAllForUserAsync(Guid userId, Guid tenantId, CancellationToken ct);
    Task<IEnumerable<RefreshToken>> GetActiveTokensForUserAsync(Guid userId, Guid tenantId, CancellationToken ct);
}

public interface ICondominioRepository
{
    Task<Condominio?> GetByIdAsync(Guid id, CancellationToken ct);
    Task<Condominio?> GetByCnpjAsync(string cnpj, CancellationToken ct);
    Task<IReadOnlyList<Condominio>> ListAsync(CancellationToken ct);
    Task AddAsync(Condominio condominio, CancellationToken ct);
    Task UpdateAsync(Condominio condominio, CancellationToken ct);

    Task<IReadOnlyList<ConformityItem>> ListConformitiesAsync(Guid condominioId, CancellationToken ct);
    Task<ConformityItem?> GetConformityAsync(Guid condominioId, Guid itemId, CancellationToken ct);
    Task UpdateConformityAsync(ConformityItem item, CancellationToken ct);
}