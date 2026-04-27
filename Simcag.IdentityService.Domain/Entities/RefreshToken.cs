namespace Simcag.IdentityService.Domain.Entities;

using Simcag.IdentityService.Domain.ValueObjects;
using Simcag.IdentityService.Domain.Results;

/// <summary>
/// Entidade de Refresh Token - Aggregate Root.
/// Representa um refresh token com suporte a multi-tenancy.
/// </summary>
public sealed class RefreshToken : IAggregateRoot
{
    public Guid Id { get; private set; }
    public string Token { get; private set; } = string.Empty;
    public Guid UserId { get; private set; }
    public TenantId TenantId { get; private set; } = null!;
    public DateTime ExpiresAt { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public bool IsRevoked { get; private set; }
    public DateTime? RevokedAt { get; private set; }

    public User? User { get; private set; }

    private RefreshToken() { } // EF Core

    private RefreshToken(string token, Guid userId, TenantId tenantId, DateTime expiresAt)
    {
        Id = Guid.NewGuid();
        Token = token;
        UserId = userId;
        TenantId = tenantId;
        ExpiresAt = expiresAt;
        CreatedAt = DateTime.UtcNow;
        IsRevoked = false;
    }

    /// <summary>
    /// Factory method para criar um refresh token com validação.
    /// </summary>
    public static Result<RefreshToken> Create(
        string token,
        Guid userId,
        Guid tenantId,
        DateTime expiresAt)
    {
        if (string.IsNullOrWhiteSpace(token))
            return Result<RefreshToken>.Fail("Token é obrigatório");

        if (userId == Guid.Empty)
            return Result<RefreshToken>.Fail("User ID inválido");

        var tenantIdResult = TenantId.Create(tenantId);
        if (tenantIdResult is Result<TenantId>.Failure f1)
            return Result<RefreshToken>.Fail(f1.Error);

        if (expiresAt <= DateTime.UtcNow)
            return Result<RefreshToken>.Fail("Data de expiração deve ser no futuro");

        return Result<RefreshToken>.Ok(new RefreshToken(
            token,
            userId,
            tenantIdResult.Match(x => x, e => throw new InvalidOperationException()),
            expiresAt));
    }

    public void Revoke()
    {
        IsRevoked = true;
        RevokedAt = DateTime.UtcNow;
    }

    public bool IsExpired() => DateTime.UtcNow >= ExpiresAt;

    public bool IsActive() => !IsRevoked && !IsExpired();

    public bool BelongsToTenant(Guid tenantId) => TenantId.Value == tenantId;

    public bool BelongsToUser(Guid userId) => UserId == userId;
}