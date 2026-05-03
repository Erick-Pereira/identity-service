namespace Simcag.IdentityService.Application.DTOs;

using System.ComponentModel.DataAnnotations;

public class RegisterRequest
{
    [Required]
    public Guid TenantId { get; set; }

    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required]
    [MinLength(8)]
    public string Password { get; set; } = string.Empty;

    [Required]
    [MinLength(1)]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    [Required]
    public string Role { get; set; } = "User"; // Default role
}

public class LoginRequest
{
    [Required]
    public Guid TenantId { get; set; }

    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required]
    [MinLength(8)]
    public string Password { get; set; } = string.Empty;
}

public class RefreshTokenRequest
{
    [Required]
    public string RefreshToken { get; set; } = string.Empty;
}

public class AuthResult
{
    public bool Success { get; set; }
    public string? AccessToken { get; set; }
    public string? RefreshToken { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public string? Error { get; set; }
    public UserProfileDto? User { get; set; }

    public static AuthResult FromCredentials(string accessToken, string refreshToken, DateTime expiresAt, UserProfileDto user)
    {
        return new AuthResult
        {
            Success = true,
            AccessToken = accessToken,
            RefreshToken = refreshToken,
            ExpiresAt = expiresAt,
            User = user
        };
    }

    public static AuthResult Failure(string error)
    {
        return new AuthResult
        {
            Success = false,
            Error = error
        };
    }
}

public class UserProfileDto
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public string Email { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public bool IsActive { get; set; }
}

/// <summary>Resposta de <c>GET /api/auth/validate</c> (corpo JSON sem envelope, para compatibilidade com clientes existentes).</summary>
public sealed class TokenValidationResponse
{
    public bool IsValid { get; set; }
    public string UserId { get; set; } = string.Empty;
    public string TenantId { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public string? Error { get; set; }
}

public class JwtTokenValidationResult
{
    public bool IsValid { get; set; }
    public Guid? UserId { get; set; }
    public Guid? TenantId { get; set; }
    public string? Email { get; set; }
    public string? Name { get; set; }
    public string? Role { get; set; }
    public string? Error { get; set; }

    public static JwtTokenValidationResult Valid(Guid userId, Guid tenantId, string email, string name, string role)
    {
        return new JwtTokenValidationResult
        {
            IsValid = true,
            UserId = userId,
            TenantId = tenantId,
            Email = email,
            Name = name,
            Role = role
        };
    }

    public static JwtTokenValidationResult Invalid(string error)
    {
        return new JwtTokenValidationResult
        {
            IsValid = false,
            Error = error
        };
    }
}