namespace Simcag.IdentityService.Application.Interfaces;

public interface IJwtTokenService
{
    /// <summary>Valor efetivo da configuração JWT (minutos).</summary>
    int AccessTokenExpirationMinutes { get; }

    /// <summary>Valor efetivo da configuração JWT (dias).</summary>
    int RefreshTokenExpirationDays { get; }

    Task<string> GenerateAccessTokenAsync(
        Guid userId,
        Guid tenantId,
        string email,
        string name,
        string role,
        CancellationToken ct);

    Task<string> GenerateRefreshTokenAsync(
        Guid userId,
        Guid tenantId,
        CancellationToken ct);

    Task<JwtValidationResult> ValidateTokenAsync(
        string token,
        CancellationToken ct);
}

public sealed record JwtValidationResult(
    bool IsValid,
    Guid? UserId,
    Guid? TenantId,
    string Email,
    string? Error)
{
    public static JwtValidationResult Invalid(string error) =>
        new(false, null, null, string.Empty, error);
}
