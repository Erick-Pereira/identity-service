namespace Simcag.IdentityService.Application.DTOs;

public class RegisterRequest
{
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Role { get; set; } = "User"; // Default role
}

public class LoginRequest
{
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}

public class RefreshTokenRequest
{
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
    public string Email { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public bool IsActive { get; set; }
}

public class JwtTokenValidationResult
{
    public bool IsValid { get; set; }
    public Guid? UserId { get; set; }
    public string? Email { get; set; }
    public string? Name { get; set; }
    public string? Role { get; set; }
    public string? Error { get; set; }

    public static JwtTokenValidationResult Valid(Guid userId, string email, string name, string role)
    {
        return new JwtTokenValidationResult
        {
            IsValid = true,
            UserId = userId,
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