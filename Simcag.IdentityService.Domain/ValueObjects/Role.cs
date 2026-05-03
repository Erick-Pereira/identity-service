namespace Simcag.IdentityService.Domain.ValueObjects;

using Simcag.IdentityService.Domain.Results;

/// <summary>
/// Value Object que representa um role (permissão) no sistema.
/// Roles permitidos: Admin, Sindico, Conselho
/// </summary>
public sealed class Role : IEquatable<Role>
{
    public const string AdminValue = "Admin";
    public const string SindicoValue = "Sindico";
    public const string ConselhoValue = "Conselho";

    public string Value { get; }

    private Role(string value)
    {
        Value = value;
    }

    /// <summary>Reidratação a partir da BD (EF). Não usar para input externo.</summary>
    public static Role FromStorage(string value) => new(value);

    public static Result<Role> Create(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return Result<Role>.Fail("Role não pode estar vazio");

        value = value.Trim();

        if (!IsValidRole(value))
            return Result<Role>.Fail($"Role '{value}' não é válido. Valores permitidos: Admin, Sindico, Conselho");

        return Result<Role>.Ok(new Role(value));
    }

    private static bool IsValidRole(string value)
        => value is AdminValue or SindicoValue or ConselhoValue;

    public static Role Admin => new(AdminValue);
    public static Role Sindico => new(SindicoValue);
    public static Role Conselho => new(ConselhoValue);

    public override bool Equals(object? obj)
        => Equals(obj as Role);

    public bool Equals(Role? other)
        => other is not null && Value == other.Value;

    public override int GetHashCode()
        => Value.GetHashCode();

    public override string ToString()
        => Value;
}
