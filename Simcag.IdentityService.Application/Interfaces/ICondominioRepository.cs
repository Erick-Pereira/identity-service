namespace Simcag.IdentityService.Application.Interfaces;

using Simcag.IdentityService.Domain.Entities;

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
