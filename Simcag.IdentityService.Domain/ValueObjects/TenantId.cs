namespace Simcag.IdentityService.Domain.ValueObjects;

using Simcag.IdentityService.Domain.Results;

/// <summary>
/// Value Object que representa o ID de um tenant (condomínio).
/// Garante que todo usuário está isolado por tenant.
/// </summary>
public sealed class TenantId : IEquatable<TenantId>
{
    public Guid Value { get; }

    private TenantId(Guid value)
    {
        Value = value;
    }

    public static Result<TenantId> Create(Guid value)
    {
        if (value == Guid.Empty)
            return Result<TenantId>.Fail("Tenant ID não pode ser vazio");

        return Result<TenantId>.Ok(new TenantId(value));
    }

    public override bool Equals(object? obj)
        => Equals(obj as TenantId);

    public bool Equals(TenantId? other)
        => other is not null && Value == other.Value;

    public override int GetHashCode()
        => Value.GetHashCode();

    public override string ToString()
        => Value.ToString();
}
