namespace Simcag.IdentityService.Domain.ValueObjects;

using Simcag.IdentityService.Domain.Results;
using BCrypt.Net;

/// <summary>
/// Value Object que representa um hash de senha seguro (BCrypt).
/// </summary>
public sealed class PasswordHash : IEquatable<PasswordHash>
{
    public string Value { get; }

    private PasswordHash(string value)
    {
        Value = value;
    }

    /// <summary>
    /// Factory method para criar um PasswordHash a partir de um valor pré-hasheado.
    /// </summary>
    public static Result<PasswordHash> CreateFromHash(string? hashedValue)
    {
        if (string.IsNullOrWhiteSpace(hashedValue))
            return Result<PasswordHash>.Fail("Hash de senha não pode estar vazio");

        if (hashedValue.Length < 50) // BCrypt hash típico tem ~60 caracteres
            return Result<PasswordHash>.Fail("Hash de senha inválido");

        return Result<PasswordHash>.Ok(new PasswordHash(hashedValue));
    }

    public bool VerifyPassword(string plainPassword)
    {
        try
        {
            return BCrypt.Verify(plainPassword, Value);
        }
        catch
        {
            return false;
        }
    }

    public override bool Equals(object? obj)
        => Equals(obj as PasswordHash);

    public bool Equals(PasswordHash? other)
        => other is not null && Value == other.Value;

    public override int GetHashCode()
        => Value.GetHashCode();

    public override string ToString()
        => "***"; // Nunca expor o hash em logs/debug
}
