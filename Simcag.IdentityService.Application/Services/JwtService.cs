using Microsoft.IdentityModel.Tokens;
using Simcag.IdentityService.Application.DTOs;
using Simcag.IdentityService.Application.Interfaces;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Simcag.IdentityService.Application.Services;

public class JwtService : IJwtService
{
    private readonly string _secretKey;
    private readonly string _issuer;
    private readonly string _audience;
    private readonly int _accessTokenExpirationMinutes;
    private readonly int _refreshTokenExpirationDays;

    public JwtService(string secretKey, string issuer, string audience, int accessTokenExpirationMinutes, int refreshTokenExpirationDays)
    {
        _secretKey = secretKey;
        _issuer = issuer;
        _audience = audience;
        _accessTokenExpirationMinutes = accessTokenExpirationMinutes;
        _refreshTokenExpirationDays = refreshTokenExpirationDays;
    }

    public async Task<string> GenerateAccessTokenAsync(Guid userId, string email, string name, string role, CancellationToken ct)
    {
        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, userId.ToString()),
            new Claim(JwtRegisteredClaimNames.Email, email),
            new Claim("name", name),
            new Claim(ClaimTypes.Role, role),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_secretKey));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _issuer,
            audience: _audience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(_accessTokenExpirationMinutes),
            signingCredentials: credentials
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public async Task<string> GenerateRefreshTokenAsync(CancellationToken ct)
    {
        var randomNumber = new byte[32];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(randomNumber);
        return Convert.ToBase64String(randomNumber);
    }

    public async Task<JwtTokenValidationResult> ValidateTokenAsync(string token, CancellationToken ct)
    {
        try
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.UTF8.GetBytes(_secretKey);

            var validationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                ValidIssuer = _issuer,
                ValidAudience = _audience,
                IssuerSigningKey = new SymmetricSecurityKey(key),
                ClockSkew = TimeSpan.Zero
            };

            var principal = tokenHandler.ValidateToken(token, validationParameters, out _);

            var userIdClaim = principal.FindFirst(JwtRegisteredClaimNames.Sub);
            var emailClaim = principal.FindFirst(JwtRegisteredClaimNames.Email);
            var nameClaim = principal.FindFirst("name");
            var roleClaim = principal.FindFirst(ClaimTypes.Role);

            if (userIdClaim == null || emailClaim == null || nameClaim == null || roleClaim == null)
            {
                return JwtTokenValidationResult.Invalid("Missing required claims");
            }

            if (!Guid.TryParse(userIdClaim.Value, out var userId))
            {
                return JwtTokenValidationResult.Invalid("Invalid user ID format");
            }

            return JwtTokenValidationResult.Valid(userId, emailClaim.Value, nameClaim.Value, roleClaim.Value);
        }
        catch (SecurityTokenExpiredException)
        {
            return JwtTokenValidationResult.Invalid("Token has expired");
        }
        catch (SecurityTokenInvalidSignatureException)
        {
            return JwtTokenValidationResult.Invalid("Invalid token signature");
        }
        catch (Exception ex)
        {
            return JwtTokenValidationResult.Invalid($"Token validation failed: {ex.Message}");
        }
    }

    public async Task RevokeRefreshTokenAsync(string token, CancellationToken ct)
    {
        // In a real implementation, this would mark the refresh token as revoked in the database
        // For now, we'll just return as the token management is handled by the repository
        await Task.CompletedTask;
    }
}