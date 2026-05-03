namespace Simcag.IdentityService.Application.Interfaces;

using Simcag.IdentityService.Application.DTOs;

public interface IAuthService
{
    Task<AuthResult> RegisterAsync(RegisterRequest request, CancellationToken ct);

    Task<AuthResult> LoginAsync(LoginRequest request, CancellationToken ct);

    Task<AuthResult> RefreshTokenAsync(RefreshTokenRequest request, CancellationToken ct);

    Task<UserProfileDto?> GetUserProfileAsync(Guid userId, Guid tenantId, CancellationToken ct);

    Task<TokenValidationResponse> ValidateAccessTokenAsync(string authorizationHeader, CancellationToken ct);
}
