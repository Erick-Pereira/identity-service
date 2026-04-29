namespace Simcag.IdentityService.Infrastructure.Repositories;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Simcag.IdentityService.Application.Interfaces;
using Simcag.IdentityService.Domain.Entities;
using Simcag.IdentityService.Infrastructure.Persistence.DbContext;

public sealed class CondominioRepository : ICondominioRepository
{
    private readonly IdentityServiceDbContext _db;
    private readonly ILogger<CondominioRepository> _logger;

    public CondominioRepository(IdentityServiceDbContext db, ILogger<CondominioRepository> logger)
    {
        _db = db;
        _logger = logger;
    }

    public Task<Condominio?> GetByIdAsync(Guid id, CancellationToken ct) =>
        _db.Condominios
            .Include(c => c.Conformities)
            .FirstOrDefaultAsync(c => c.Id == id, ct);

    public Task<Condominio?> GetByCnpjAsync(string cnpj, CancellationToken ct)
    {
        var normalized = new string([.. (cnpj ?? string.Empty).Where(char.IsDigit)]);
        return _db.Condominios
            .Include(c => c.Conformities)
            .FirstOrDefaultAsync(c => c.Cnpj == normalized, ct);
    }

    public async Task<IReadOnlyList<Condominio>> ListAsync(CancellationToken ct)
    {
        var list = await _db.Condominios
            .AsNoTracking()
            .OrderBy(c => c.Nome)
            .ToListAsync(ct);
        return list;
    }

    public async Task AddAsync(Condominio condominio, CancellationToken ct)
    {
        await _db.Condominios.AddAsync(condominio, ct);
        await _db.SaveChangesAsync(ct);
        _logger.LogInformation("Condominio criado {CondominioId}", condominio.Id);
    }

    public async Task UpdateAsync(Condominio condominio, CancellationToken ct)
    {
        _db.Condominios.Update(condominio);
        await _db.SaveChangesAsync(ct);
    }

    public async Task<IReadOnlyList<ConformityItem>> ListConformitiesAsync(Guid condominioId, CancellationToken ct)
    {
        var list = await _db.ConformityItems
            .AsNoTracking()
            .Where(c => c.CondominioId == condominioId)
            .OrderBy(c => c.DueDate)
            .ToListAsync(ct);
        return list;
    }

    public Task<ConformityItem?> GetConformityAsync(Guid condominioId, Guid itemId, CancellationToken ct) =>
        _db.ConformityItems.FirstOrDefaultAsync(c => c.CondominioId == condominioId && c.Id == itemId, ct);

    public async Task UpdateConformityAsync(ConformityItem item, CancellationToken ct)
    {
        _db.ConformityItems.Update(item);
        await _db.SaveChangesAsync(ct);
    }
}
