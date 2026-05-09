using System;
using System.Collections.Generic;

namespace Simcag.IdentityService.Domain.Entities;

/// <summary>
/// Root tenant aggregate — a condominium entity.
/// Each <see cref="User"/> (except cross-tenant ADMIN) belongs to one <see cref="Condominium"/>.
/// </summary>
public sealed class Condominium : IAggregateRoot
{
    public Guid Id { get; private set; }
    public string Cnpj { get; private set; } = null!;
    public string Name { get; private set; } = null!;
    public string Address { get; private set; } = null!;
    public string Phone { get; private set; } = string.Empty;
    public bool IsActive { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }

    private readonly List<ConformityItem> _conformities = new();
    public IReadOnlyCollection<ConformityItem> Conformities => _conformities.AsReadOnly();

    private Condominium() { }

    private Condominium(string cnpj, string name, string address, string phone)
    {
        Id = Guid.NewGuid();
        Cnpj = cnpj;
        Name = name.Trim();
        Address = address.Trim();
        Phone = (phone ?? string.Empty).Trim();
        IsActive = true;
        CreatedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    public static Condominium Create(string cnpj, string name, string address, string? phone = null)
    {
        if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("Name is required.");
        if (string.IsNullOrWhiteSpace(address)) throw new ArgumentException("Address is required.");
        var normalizedCnpj = NormalizeCnpj(cnpj);
        if (normalizedCnpj is null) throw new ArgumentException("Invalid CNPJ.", nameof(cnpj));

        var condo = new Condominium(normalizedCnpj, name, address, phone ?? string.Empty);
        condo.SeedDefaultConformities();
        return condo;
    }

    public void Update(string name, string address, string? phone)
    {
        if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("Name is required.");
        if (string.IsNullOrWhiteSpace(address)) throw new ArgumentException("Address is required.");

        Name = name.Trim();
        Address = address.Trim();
        Phone = (phone ?? Phone).Trim();
        UpdatedAt = DateTime.UtcNow;
    }

    public void Deactivate()
    {
        IsActive = false;
        UpdatedAt = DateTime.UtcNow;
    }

    public ConformityItem AddCustomConformity(string description, DateTime? dueDate)
    {
        var item = ConformityItem.CreateCustom(Id, description, dueDate);
        _conformities.Add(item);
        UpdatedAt = DateTime.UtcNow;
        return item;
    }

    private void SeedDefaultConformities()
    {
        var due = DateTime.UtcNow.AddYears(1);
        _conformities.Add(ConformityItem.CreateDefault(Id, ConformityType.Prefeitura, "Registro / atualização cadastral na prefeitura", due));
        _conformities.Add(ConformityItem.CreateDefault(Id, ConformityType.Licenca, "Licença de funcionamento", due));
        _conformities.Add(ConformityItem.CreateDefault(Id, ConformityType.AuditoriaContabil, "Auditoria contábil anual obrigatória", due));
        _conformities.Add(ConformityItem.CreateDefault(Id, ConformityType.SeguroPredial, "Apólice de seguro predial vigente", due));
        _conformities.Add(ConformityItem.CreateDefault(Id, ConformityType.CertificadoSeguranca, "AVCB - Auto de Vistoria do Corpo de Bombeiros", due));
    }

    private static string? NormalizeCnpj(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw)) return null;
        var digits = new string([.. raw.Where(char.IsDigit)]);
        return digits.Length == 14 ? digits : null;
    }
}
