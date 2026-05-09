namespace Simcag.IdentityService.Application.Interfaces;

using Simcag.IdentityService.Domain.Entities;

public interface ICondominiumRepository
{
    Task<Condominium?> GetByIdAsync(Guid id, CancellationToken ct);
    Task<Condominium?> GetByCnpjAsync(string cnpj, CancellationToken ct);
    Task<IReadOnlyList<Condominium>> ListAsync(CancellationToken ct);
    Task AddAsync(Condominium condominium, CancellationToken ct);
    Task UpdateAsync(Condominium condominium, CancellationToken ct);
    Task<IReadOnlyList<ConformityItem>> ListConformitiesAsync(Guid condominiumId, CancellationToken ct);
    Task<ConformityItem?> GetConformityAsync(Guid condominiumId, Guid itemId, CancellationToken ct);
    Task UpdateConformityAsync(ConformityItem item, CancellationToken ct);
}
