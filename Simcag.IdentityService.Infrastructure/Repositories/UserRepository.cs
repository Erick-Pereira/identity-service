namespace Simcag.IdentityService.Infrastructure.Repositories;

using Microsoft.EntityFrameworkCore;
using Simcag.IdentityService.Application.Interfaces;
using Simcag.IdentityService.Domain.Entities;
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
        var tenantVo = TenantId.FromStorage(tenantId);
        return await _dbContext.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == id && u.TenantId == tenantVo, ct);
    }

    public async Task<User?> GetByEmailAndTenantAsync(string email, Guid tenantId, CancellationToken ct)
    {
        var normalized = email.Trim().ToLowerInvariant();
        var tenantVo = TenantId.FromStorage(tenantId);
        var emailVo = Email.FromStorage(normalized);
        return await _dbContext.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Email == emailVo && u.TenantId == tenantVo && u.IsActive, ct);
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
        var normalized = email.Trim().ToLowerInvariant();
        var tenantVo = TenantId.FromStorage(tenantId);
        var emailVo = Email.FromStorage(normalized);
        return await _dbContext.Users
            .AsNoTracking()
            .AnyAsync(u => u.Email == emailVo && u.TenantId == tenantVo, ct);
    }
}