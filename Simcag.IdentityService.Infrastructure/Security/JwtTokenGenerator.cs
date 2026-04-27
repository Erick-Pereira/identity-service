namespace Simcag.IdentityService.Infrastructure.Security;

using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using Simcag.IdentityService.Application.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

public sealed class JwtTokenService : IJwtTokenService
{
    private readonly string _secretKey;
    private readonly string _issuer;
    private readonly string _audience;
    private readonly ILogger<JwtTokenService> _logger;

    public int AccessTokenExpirationMinutes { get; }
    public int RefreshTokenExpirationDays { get; }

    public JwtTokenService(
        IConfiguration configuration,
        ILogger<JwtTokenService> logger)
    {
        var section = configuration.GetSection("Jwt");
        _secretKey = section["Secret"] ?? throw new InvalidOperationException("JWT Secret não configurado");
        _issuer = section["Issuer"] ?? "Simcag.IdentityService";
        _audience = section["Audience"] ?? "Simcag.Clients";
        AccessTokenExpirationMinutes = int.Parse(section["AccessTokenExpirationMinutes"] ?? "15");
        RefreshTokenExpirationDays = int.Parse(section["RefreshTokenExpirationDays"] ?? "7");
        _logger = logger;

        ValidateConfiguration();
    }

    private void ValidateConfiguration()
    {
        if (_secretKey.Length < 32)
            throw new InvalidOperationException("JWT Secret deve ter pelo menos 32 caracteres");

        if (AccessTokenExpirationMinutes <= 0)
            throw new InvalidOperationException("Access Token expiration deve ser > 0");

        if (RefreshTokenExpirationDays <= 0)
            throw new InvalidOperationException("Refresh Token expiration deve ser > 0");
    }

    public async Task<string> GenerateAccessTokenAsync(
        Guid userId,
        Guid tenantId,
        string email,
        string name,
        string role,
        CancellationToken ct)
    {
        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, userId.ToString()),
            new Claim("tenant_id", tenantId.ToString()),
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
            expires: DateTime.UtcNow.AddMinutes(AccessTokenExpirationMinutes),
            signingCredentials: credentials);

        var tokenValue = new JwtSecurityTokenHandler().WriteToken(token);
        _logger.LogDebug("Access token gerado para usuário: {UserId}", userId);

        return await Task.FromResult(tokenValue);
    }

    public async Task<string> GenerateRefreshTokenAsync(
        Guid userId,
        Guid tenantId,
        CancellationToken ct)
    {
        var randomNumber = new byte[32];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(randomNumber);

        var token = Convert.ToBase64String(randomNumber);
        _logger.LogDebug("Refresh token gerado para usuário: {UserId}", userId);

        return await Task.FromResult(token);
    }

    public async Task<JwtValidationResult> ValidateTokenAsync(
        string token,
        CancellationToken ct)
    {
        try
        {
            // Preservar nomes curtos JWT (sub, email, …); com MapInboundClaims=true o "sub" vira NameIdentifier e falha o FindFirst abaixo.
            var tokenHandler = new JwtSecurityTokenHandler { MapInboundClaims = false };
            var key = Encoding.UTF8.GetBytes(_secretKey);

            var principal = tokenHandler.ValidateToken(token, new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(key),
                ValidateIssuer = true,
                ValidIssuer = _issuer,
                ValidateAudience = true,
                ValidAudience = _audience,
                ValidateLifetime = true,
                ClockSkew = TimeSpan.Zero
            }, out var validatedToken);

            var userIdClaim = principal.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;
            var tenantIdClaim = principal.FindFirst("tenant_id")?.Value;
            var emailClaim = principal.FindFirst(JwtRegisteredClaimNames.Email)?.Value;
            var nameClaim = principal.FindFirst("name")?.Value;
            var roleClaim = principal.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value;

            if (string.IsNullOrEmpty(userIdClaim) || string.IsNullOrEmpty(tenantIdClaim))
                return JwtValidationResult.Invalid("Claims obrigatórios faltando");

            var expires = validatedToken is JwtSecurityToken jwt ? jwt.ValidTo : (DateTime?)null;

            return new JwtValidationResult(
                IsValid: true,
                UserId: Guid.Parse(userIdClaim),
                TenantId: Guid.Parse(tenantIdClaim),
                Email: emailClaim ?? string.Empty,
                Name: nameClaim,
                Role: roleClaim,
                ExpiresAtUtc: expires,
                Error: null);
        }
        catch (SecurityTokenExpiredException)
        {
            _logger.LogWarning("Token expirado");
            return JwtValidationResult.Invalid("Token expirado");
        }
        catch (SecurityTokenInvalidSignatureException)
        {
            _logger.LogWarning("Assinatura do token inválida");
            return JwtValidationResult.Invalid("Assinatura do token inválida");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao validar token");
            return JwtValidationResult.Invalid("Falha ao validar token");
        }
    }
}
