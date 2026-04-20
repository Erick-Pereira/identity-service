using BCrypt.Net;
using Simcag.IdentityService.Application.DTOs;
using Simcag.IdentityService.Application.Interfaces;
using Simcag.IdentityService.Domain.Entities;
using System.Threading;
using System.Threading.Tasks;

namespace Simcag.IdentityService.Application.Services;

public class AuthService : IAuthService
{
    private readonly IUserRepository _userRepository;
    private readonly IRefreshTokenRepository _refreshTokenRepository;
    private readonly IJwtService _jwtService;

    public AuthService(
        IUserRepository userRepository,
        IRefreshTokenRepository refreshTokenRepository,
        IJwtService jwtService)
    {
        _userRepository = userRepository;
        _refreshTokenRepository = refreshTokenRepository;
        _jwtService = jwtService;
    }

    public async Task<AuthResult> RegisterAsync(RegisterRequest request, CancellationToken ct)
    {
        try
        {
            // Check if user already exists
            var existingUser = await _userRepository.GetByEmailAsync(request.Email, ct);
            if (existingUser != null)
            {
                return AuthResult.Failure("User with this email already exists");
            }

            // Validate role
            if (!Enum.TryParse<UserRole>(request.Role, true, out var role))
            {
                role = UserRole.User; // Default to User role
            }

            // Hash password
            var passwordHash = BCrypt.Net.BCrypt.HashPassword(request.Password);

            // Create user
            var user = User.Create(request.Email, passwordHash, request.Name, role);
            await _userRepository.AddAsync(user, ct);

            // Generate tokens
            var accessToken = await _jwtService.GenerateAccessTokenAsync(
                user.Id, user.Email, user.Name, user.Role.ToString(), ct);
            var refreshToken = await _jwtService.GenerateRefreshTokenAsync(ct);

            // Save refresh token
            var refreshTokenEntity = RefreshToken.Create(
                refreshToken,
                user.Id,
                DateTime.UtcNow.AddDays(7)); // 7 days expiration
            await _refreshTokenRepository.AddAsync(refreshTokenEntity, ct);

            var userProfile = new UserProfileDto
            {
                Id = user.Id,
                Email = user.Email,
                Name = user.Name,
                Role = user.Role.ToString(),
                CreatedAt = user.CreatedAt,
                IsActive = user.IsActive
            };

            return AuthResult.FromCredentials(accessToken, refreshToken, DateTime.UtcNow.AddMinutes(15), userProfile);
        }
        catch (Exception ex)
        {
            return AuthResult.Failure($"Registration failed: {ex.Message}");
        }
    }

    public async Task<AuthResult> LoginAsync(LoginRequest request, CancellationToken ct)
    {
        try
        {
            // Find user by email
            var user = await _userRepository.GetByEmailAsync(request.Email, ct);
            if (user == null || !user.IsActive)
            {
                return AuthResult.Failure("Invalid email or password");
            }

            // Verify password
            if (!BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
            {
                return AuthResult.Failure("Invalid email or password");
            }

            // Generate tokens
            var accessToken = await _jwtService.GenerateAccessTokenAsync(
                user.Id, user.Email, user.Name, user.Role.ToString(), ct);
            var refreshToken = await _jwtService.GenerateRefreshTokenAsync(ct);

            // Save refresh token (invalidate old ones for this user)
            await _refreshTokenRepository.RevokeAllForUserAsync(user.Id, ct);
            var refreshTokenEntity = RefreshToken.Create(
                refreshToken,
                user.Id,
                DateTime.UtcNow.AddDays(7));
            await _refreshTokenRepository.AddAsync(refreshTokenEntity, ct);

            var userProfile = new UserProfileDto
            {
                Id = user.Id,
                Email = user.Email,
                Name = user.Name,
                Role = user.Role.ToString(),
                CreatedAt = user.CreatedAt,
                IsActive = user.IsActive
            };

            return AuthResult.FromCredentials(accessToken, refreshToken, DateTime.UtcNow.AddMinutes(15), userProfile);
        }
        catch (Exception ex)
        {
            return AuthResult.Failure($"Login failed: {ex.Message}");
        }
    }

    public async Task<AuthResult> RefreshTokenAsync(RefreshTokenRequest request, CancellationToken ct)
    {
        try
        {
            // Find and validate refresh token
            var refreshToken = await _refreshTokenRepository.GetByTokenAsync(request.RefreshToken, ct);
            if (refreshToken == null || !refreshToken.IsActive())
            {
                return AuthResult.Failure("Invalid or expired refresh token");
            }

            // Get user
            var user = await _userRepository.GetByIdAsync(refreshToken.UserId, ct);
            if (user == null || !user.IsActive)
            {
                return AuthResult.Failure("User not found or inactive");
            }

            // Revoke old refresh token
            refreshToken.Revoke();
            await _refreshTokenRepository.UpdateAsync(refreshToken, ct);

            // Generate new tokens
            var accessToken = await _jwtService.GenerateAccessTokenAsync(
                user.Id, user.Email, user.Name, user.Role.ToString(), ct);
            var newRefreshToken = await _jwtService.GenerateRefreshTokenAsync(ct);

            // Save new refresh token
            var newRefreshTokenEntity = RefreshToken.Create(
                newRefreshToken,
                user.Id,
                DateTime.UtcNow.AddDays(7));
            await _refreshTokenRepository.AddAsync(newRefreshTokenEntity, ct);

            var userProfile = new UserProfileDto
            {
                Id = user.Id,
                Email = user.Email,
                Name = user.Name,
                Role = user.Role.ToString(),
                CreatedAt = user.CreatedAt,
                IsActive = user.IsActive
            };

            return AuthResult.FromCredentials(accessToken, newRefreshToken, DateTime.UtcNow.AddMinutes(15), userProfile);
        }
        catch (Exception ex)
        {
            return AuthResult.Failure($"Token refresh failed: {ex.Message}");
        }
    }

    public async Task<UserProfileDto?> GetUserProfileAsync(Guid userId, CancellationToken ct)
    {
        var user = await _userRepository.GetByIdAsync(userId, ct);
        if (user == null)
        {
            return null;
        }

        return new UserProfileDto
        {
            Id = user.Id,
            Email = user.Email,
            Name = user.Name,
            Role = user.Role.ToString(),
            CreatedAt = user.CreatedAt,
            IsActive = user.IsActive
        };
    }
}
