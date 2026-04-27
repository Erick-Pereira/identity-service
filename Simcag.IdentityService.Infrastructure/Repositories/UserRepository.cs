namespace Simcag.IdentityService.Infrastructure.Repositories;

using Microsoft.EntityFrameworkCore;
using Simcag.IdentityService.Application.Interfaces;
using Simcag.IdentityService.Domain.Entities;
using Simcag.IdentityService.Domain.Results;
using Simcag.IdentityService.Domain.ValueObjects;
using Simcag.IdentityService.Infrastructure.Persistence.DbContext;
using Microsoft.Extensions.Logging;

public sealed class UserRepository : IUserRepository
{
    private readonly IdentityServiceDbContext _dbContext;
    private readonly ILogger<UserRepository> _logger;

    public UserRepository(IdentityServiceDbContext dbContext, ILogger<UserRepository> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<User?> GetByIdAsync(Guid id, Guid tenantId, CancellationToken ct)
    {
        if (TenantId.Create(tenantId) is not Result<TenantId>.Success tenantOk)
            return null;

        return await _dbContext.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == id && u.TenantId == tenantOk.Value, ct);
    }

    public async Task<User?> GetByEmailAndTenantAsync(string email, Guid tenantId, CancellationToken ct)
    {
        // Comparar propriedades com tipo do modelo (Email, TenantId), não .Value — assim o EF Core
        // aplica HasConversion e traduz para a coluna (Npgsql).
        if (Email.Create(email) is not Result<Email>.Success emailOk)
            return null;

        if (TenantId.Create(tenantId) is not Result<TenantId>.Success tenantOk)
            return null;

        return await _dbContext.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u =>
                u.Email == emailOk.Value &&
                u.TenantId == tenantOk.Value &&
                u.IsActive, ct);
    }

    public async Task AddAsync(User user, CancellationToken ct)
    {
        await _dbContext.Users.AddAsync(user, ct);
        await _dbContext.SaveChangesAsync(ct);
        _logger.LogInformation("Usuário adicionado: {UserId}, Tenant: {TenantId}", user.Id, user.TenantId.Value);
    }

    public async Task UpdateAsync(User user, CancellationToken ct)
    {
        _dbContext.Users.Update(user);
        await _dbContext.SaveChangesAsync(ct);
        _logger.LogInformation("Usuário atualizado: {UserId}", user.Id);
    }

    public async Task<bool> EmailExistsAsync(string email, Guid tenantId, CancellationToken ct)
    {
        if (Email.Create(email) is not Result<Email>.Success emailOk)
            return false;

        if (TenantId.Create(tenantId) is not Result<TenantId>.Success tenantOk)
            return false;

        return await _dbContext.Users
            .AsNoTracking()
            .AnyAsync(u =>
                u.Email == emailOk.Value &&
                u.TenantId == tenantOk.Value, ct);
    }
}