namespace Simcag.IdentityService.Domain.Repositories;
using Simcag.IdentityService.Domain.Aggregates;

public interface IConformanceRepository
{
    Task<List<ConformanceItem>> GetByCondominioAsync(Guid condominioId);
    Task<ConformanceItem?> GetByIdAsync(Guid id, Guid condominioId);
    Task AddAsync(ConformanceItem item);
    Task UpdateAsync(ConformanceItem item);
    Task DeleteAsync(Guid id);
}
