namespace Simcag.IdentityService.Application.Interfaces;

public interface IJwtTokenService
{
    /// <summary>Tempos de expiração configurados (fonte única para JWT emitido e para <c>ExpiresAt</c> nas respostas).</summary>
    int AccessTokenExpirationMinutes { get; }
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
    string? Name,
    string? Role,
    DateTime? ExpiresAtUtc,
    string? Error)
{
    public static JwtValidationResult Invalid(string error) =>
        new(false, null, null, string.Empty, null, null, null, error);
}
