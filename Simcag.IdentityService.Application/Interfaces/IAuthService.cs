using Simcag.IdentityService.Application.DTOs;
using System.Threading;
using System.Threading.Tasks;

namespace Simcag.IdentityService.Application.Interfaces;

public interface IAuthService
{
    Task<AuthResult> RegisterAsync(RegisterRequest request, CancellationToken ct);
    Task<AuthResult> LoginAsync(LoginRequest request, CancellationToken ct);
    Task<AuthResult> RefreshTokenAsync(RefreshTokenRequest request, CancellationToken ct);
    Task<UserProfileDto?> GetUserProfileAsync(Guid userId, CancellationToken ct);
}

public interface IJwtService
{
    Task<string> GenerateAccessTokenAsync(Guid userId, string email, string name, string role, CancellationToken ct);
    Task<string> GenerateRefreshTokenAsync(CancellationToken ct);
    Task<JwtTokenValidationResult> ValidateTokenAsync(string token, CancellationToken ct);
    Task RevokeRefreshTokenAsync(string token, CancellationToken ct);
}