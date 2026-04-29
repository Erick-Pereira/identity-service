using System;
using System.Collections.Generic;

namespace Simcag.IdentityService.Domain.Entities;

/// <summary>
/// Tenant principal do sistema — agregado raiz que representa um condomínio.
/// Cada <see cref="User"/> (exceto ADMIN) pertence a um <see cref="Condominio"/>.
/// Toda query de qualquer serviço é filtrada por <see cref="Id"/>.
/// </summary>
public sealed class Condominio : IAggregateRoot
{
    public Guid Id { get; private set; }
    public string Cnpj { get; private set; } = null!;
    public string Nome { get; private set; } = null!;
    public string Endereco { get; private set; } = null!;
    public string Telefone { get; private set; } = string.Empty;
    public bool IsActive { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }

    private readonly List<ConformityItem> _conformities = new();
    public IReadOnlyCollection<ConformityItem> Conformities => _conformities.AsReadOnly();

    private Condominio() { }

    private Condominio(string cnpj, string nome, string endereco, string telefone)
    {
        Id = Guid.NewGuid();
        Cnpj = cnpj;
        Nome = nome.Trim();
        Endereco = endereco.Trim();
        Telefone = (telefone ?? string.Empty).Trim();
        IsActive = true;
        CreatedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    public static Condominio Create(string cnpj, string nome, string endereco, string? telefone = null)
    {
        if (string.IsNullOrWhiteSpace(nome)) throw new ArgumentException("Nome obrigatório.");
        if (string.IsNullOrWhiteSpace(endereco)) throw new ArgumentException("Endereço obrigatório.");
        var normalizedCnpj = NormalizeCnpj(cnpj);
        if (normalizedCnpj is null) throw new ArgumentException("CNPJ inválido.", nameof(cnpj));

        var condo = new Condominio(normalizedCnpj, nome, endereco, telefone ?? string.Empty);
        condo.SeedDefaultConformities();
        return condo;
    }

    public void Update(string nome, string endereco, string? telefone)
    {
        if (string.IsNullOrWhiteSpace(nome)) throw new ArgumentException("Nome obrigatório.");
        if (string.IsNullOrWhiteSpace(endereco)) throw new ArgumentException("Endereço obrigatório.");

        Nome = nome.Trim();
        Endereco = endereco.Trim();
        Telefone = (telefone ?? Telefone).Trim();
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
