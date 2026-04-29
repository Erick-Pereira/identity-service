namespace Simcag.IdentityService.Application.DTOs;

using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

/// <summary>
/// Cria um condomínio e o seu primeiro usuário ADMIN em uma única operação.
/// Use este endpoint quando o condomínio ainda não existe no sistema.
/// </summary>
public sealed class SetupRequest : IValidatableObject
{
    // ── Condomínio ───────────────────────────────────────────────────────────
    [Required(ErrorMessage = "CNPJ obrigatório.")]
    public string Cnpj { get; set; } = string.Empty;

    [Required(ErrorMessage = "Nome do condomínio obrigatório.")]
    [MaxLength(200)]
    public string Nome { get; set; } = string.Empty;

    [Required(ErrorMessage = "Endereço obrigatório.")]
    [MaxLength(500)]
    public string Endereco { get; set; } = string.Empty;

    public string? Telefone { get; set; }

    // ── Primeiro ADMIN ───────────────────────────────────────────────────────
    [Required(ErrorMessage = "E-mail do administrador obrigatório.")]
    [EmailAddress]
    public string AdminEmail { get; set; } = string.Empty;

    [Required]
    [MinLength(8, ErrorMessage = "Senha mínima de 8 caracteres.")]
    public string AdminPassword { get; set; } = string.Empty;

    [Required]
    [MaxLength(100)]
    public string AdminName { get; set; } = string.Empty;

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        var digits = new string(Cnpj.Where(char.IsDigit).ToArray());
        if (digits.Length != 14)
            yield return new ValidationResult("CNPJ deve conter 14 dígitos.", [nameof(Cnpj)]);
    }
}

public sealed class SetupResult
{
    public bool Success { get; init; }
    public string? Error { get; init; }
    public Guid CondominioId { get; init; }
    public string? AccessToken { get; init; }
    public string? RefreshToken { get; init; }
    public DateTime? ExpiresAt { get; init; }
    public UserProfileDto? User { get; init; }
}

public sealed class RegisterRequest : IValidatableObject
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
    /// <summary>Valores do domínio: Admin, Sindico, Conselho.</summary>
    public string Role { get; set; } = "Sindico";

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (TenantId == Guid.Empty)
            yield return new ValidationResult(
                "TenantId não pode ser vazio (use o identificador do condomínio).",
                new[] { nameof(TenantId) });
    }
}

public sealed class LoginRequest : IValidatableObject
{
    [Required]
    public Guid TenantId { get; set; }

    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required]
    [MinLength(8)]
    public string Password { get; set; } = string.Empty;

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (TenantId == Guid.Empty)
            yield return new ValidationResult(
                "TenantId não pode ser vazio (use o identificador do condomínio).",
                new[] { nameof(TenantId) });
    }
}

public class RefreshTokenRequest
{
    [Required]
    public string RefreshToken { get; set; } = string.Empty;
}

public class LogoutRequest
{
    /// <summary>Se preenchido, o refresh token é revogado no servidor.</summary>
    public string? RefreshToken { get; set; }
}

/// <summary>Resposta de <c>GET /api/auth/validate</c> (compatível com introspecção por gateway/clientes).</summary>
public sealed class TokenValidationResponse
{
    public bool IsValid { get; init; }
    public string UserId { get; init; } = string.Empty;
    /// <summary>Identificador do condomínio (tenant) associado ao token.</summary>
    public string TenantId { get; init; } = string.Empty;
    public string UserName { get; init; } = string.Empty;
    public string Role { get; init; } = string.Empty;
    public IReadOnlyList<string> Permissions { get; init; } = Array.Empty<string>();
    public DateTime ExpiresAt { get; init; }
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