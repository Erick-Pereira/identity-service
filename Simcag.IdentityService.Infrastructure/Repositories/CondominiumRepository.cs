namespace Simcag.IdentityService.Infrastructure.Repositories;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Simcag.IdentityService.Application.Interfaces;
using Simcag.IdentityService.Domain.Entities;
using Simcag.IdentityService.Infrastructure.Persistence.DbContext;

public sealed class CondominiumRepository : ICondominiumRepository
{
    private readonly IdentityServiceDbContext _db;
    private readonly ILogger<CondominiumRepository> _logger;

    public CondominiumRepository(IdentityServiceDbContext db, ILogger<CondominiumRepository> logger)
    {
        _db = db;
        _logger = logger;
    }

    public Task<Condominium?> GetByIdAsync(Guid id, CancellationToken ct) =>
        _db.Condominiums
            .Include(c => c.Conformities)
            .FirstOrDefaultAsync(c => c.Id == id, ct);

    public Task<Condominium?> GetByCnpjAsync(string cnpj, CancellationToken ct)
    {
        var normalized = new string([.. (cnpj ?? string.Empty).Where(char.IsDigit)]);
        return _db.Condominiums
            .Include(c => c.Conformities)
            .FirstOrDefaultAsync(c => c.Cnpj == normalized, ct);
    }

    public async Task<IReadOnlyList<Condominium>> ListAsync(CancellationToken ct)
    {
        var list = await _db.Condominiums
            .AsNoTracking()
            .OrderBy(c => c.Name)
            .ToListAsync(ct);
        return list;
    }

    public async Task AddAsync(Condominium condominium, CancellationToken ct)
    {
        await _db.Condominiums.AddAsync(condominium, ct);
        await _db.SaveChangesAsync(ct);
        _logger.LogInformation("Condominium created {CondominiumId}", condominium.Id);
    }

    public async Task UpdateAsync(Condominium condominium, CancellationToken ct)
    {
        _db.Condominiums.Update(condominium);
        await _db.SaveChangesAsync(ct);
    }

    public async Task<IReadOnlyList<ConformityItem>> ListConformitiesAsync(Guid condominiumId, CancellationToken ct)
    {
        var list = await _db.ConformityItems
            .AsNoTracking()
            .Where(c => c.CondominiumId == condominiumId)
            .OrderBy(c => c.DueDate)
            .ToListAsync(ct);
        return list;
    }

    public Task<ConformityItem?> GetConformityAsync(Guid condominiumId, Guid itemId, CancellationToken ct) =>
        _db.ConformityItems.FirstOrDefaultAsync(c => c.CondominiumId == condominiumId && c.Id == itemId, ct);

    public async Task UpdateConformityAsync(ConformityItem item, CancellationToken ct)
    {
        _db.ConformityItems.Update(item);
        await _db.SaveChangesAsync(ct);
    }
}
