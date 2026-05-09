using System;

namespace Simcag.IdentityService.Domain.Entities;

/// <summary>
/// Regulatory conformity item for a condominium.
/// Status is computed: <see cref="ConformityStatus.Completed"/> /
/// <see cref="ConformityStatus.Overdue"/> / <see cref="ConformityStatus.Pending"/>.
/// </summary>
public sealed class ConformityItem
{
    public Guid Id { get; private set; }
    public Guid CondominiumId { get; private set; }
    public ConformityType Type { get; private set; }
    public string Description { get; private set; } = string.Empty;
    public DateTime? DueDate { get; private set; }
    public DateTime? CompletedAt { get; private set; }
    public string? Notes { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }

    public ConformityStatus Status =>
        CompletedAt.HasValue ? ConformityStatus.Completed
        : DueDate.HasValue && DueDate.Value < DateTime.UtcNow ? ConformityStatus.Overdue
        : ConformityStatus.Pending;

    private ConformityItem() { }

    internal static ConformityItem CreateDefault(Guid condominiumId, ConformityType type, string description, DateTime? dueDate)
    {
        if (type == ConformityType.Custom) throw new ArgumentException("Use CreateCustom for CUSTOM type.");
        return new ConformityItem
        {
            Id = Guid.NewGuid(),
            CondominiumId = condominiumId,
            Type = type,
            Description = description.Trim(),
            DueDate = dueDate,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
    }

    internal static ConformityItem CreateCustom(Guid condominiumId, string description, DateTime? dueDate)
    {
        if (string.IsNullOrWhiteSpace(description))
            throw new ArgumentException("Description is required for a custom item.");

        return new ConformityItem
        {
            Id = Guid.NewGuid(),
            CondominiumId = condominiumId,
            Type = ConformityType.Custom,
            Description = description.Trim(),
            DueDate = dueDate,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
    }

    public void MarkCompleted(string? notes = null)
    {
        CompletedAt = DateTime.UtcNow;
        Notes = notes?.Trim();
        UpdatedAt = DateTime.UtcNow;
    }

    public void Reopen()
    {
        CompletedAt = null;
        UpdatedAt = DateTime.UtcNow;
    }

    public void UpdateDueDate(DateTime? dueDate)
    {
        DueDate = dueDate;
        UpdatedAt = DateTime.UtcNow;
    }

    public void UpdateNotes(string? notes)
    {
        Notes = notes?.Trim();
        UpdatedAt = DateTime.UtcNow;
    }
}

public enum ConformityType
{
    Prefeitura = 1,
    Licenca = 2,
    AuditoriaContabil = 3,
    SeguroPredial = 4,
    CertificadoSeguranca = 5,
    Custom = 99
}

public enum ConformityStatus
{
    Pending = 1,
    Completed = 2,
    Overdue = 3
}
