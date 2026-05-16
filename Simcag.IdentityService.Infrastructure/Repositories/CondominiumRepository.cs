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
        // Não chamar Update() quando o agregado já está rastreado (ex.: vindo de GetByIdAsync + Include).
        // Update() percorre o grafo e trata filhos com chave já definida como Modified; itens novos
        // (ex.: AddCustomConformity) acabam em UPDATE em vez de INSERT → 0 linhas → DbUpdateConcurrencyException.
        if (_db.Entry(condominium).State == EntityState.Detached)
            _db.Condominiums.Update(condominium);

        await FixMisclassifiedNewConformityItemsAsync(ct);

        await _db.SaveChangesAsync(ct);
    }

    /// <summary>
    /// Itens criados no domínio com Id (Guid) já preenchido podem ser rastreados como <see cref="EntityState.Modified"/>
    /// em vez de <see cref="EntityState.Added"/>, gerando UPDATE em linha inexistente. Corrige antes de SaveChanges.
    /// </summary>
    private async Task FixMisclassifiedNewConformityItemsAsync(CancellationToken ct)
    {
        var modifiedEntries = _db.ChangeTracker
            .Entries<ConformityItem>()
            .Where(e => e.State == EntityState.Modified)
            .ToList();
        if (modifiedEntries.Count == 0) return;

        var ids = modifiedEntries.Select(e => e.Entity.Id).Distinct().ToList();
        var existing = await _db.ConformityItems
            .AsNoTracking()
            .Where(c => ids.Contains(c.Id))
            .Select(c => c.Id)
            .ToListAsync(ct);
        var existingSet = existing.ToHashSet();

        foreach (var entry in modifiedEntries)
        {
            if (!existingSet.Contains(entry.Entity.Id))
                entry.State = EntityState.Added;
        }
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
