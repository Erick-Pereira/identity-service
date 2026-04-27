namespace Simcag.IdentityService.Domain.ValueObjects;

using Simcag.IdentityService.Domain.Results;

/// <summary>
/// Value Object que representa um email válido.
/// Encapsula validação RFC 5322 simplificada e normalização.
/// </summary>
public sealed class Email : IEquatable<Email>
{
    public string Value { get; }

    private Email(string value)
    {
        Value = value;
    }

    /// <summary>
    /// Factory method para criar um Email com validação.
    /// </summary>
    public static Result<Email> Create(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return Result<Email>.Fail("Email não pode estar vazio");

        value = value.Trim().ToLowerInvariant();

        if (value.Length > 254)
            return Result<Email>.Fail("Email não pode ter mais de 254 caracteres");

        if (!IsValidEmailFormat(value))
            return Result<Email>.Fail("Formato de email inválido");

        return Result<Email>.Ok(new Email(value));
    }

    private static bool IsValidEmailFormat(string email)
    {
        try
        {
            var addr = new System.Net.Mail.MailAddress(email);
            return addr.Address == email;
        }
        catch
        {
            return false;
        }
    }

    public override bool Equals(object? obj)
        => Equals(obj as Email);

    public bool Equals(Email? other)
        => other is not null && Value == other.Value;

    public override int GetHashCode()
        => Value.GetHashCode();

    public override string ToString()
        => Value;
}
