namespace Simcag.IdentityService.Application.DTOs;

/// <summary>Public condominium lookup row —also used by GET /api/condominios/lookup (anonymous).</summary>
public sealed class CondominiumLookupDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    /// <summary>CNPJ formatted (XX.XXX.XXX/XXXX-XX).</summary>
    public string Cnpj { get; set; } = string.Empty;
}

public sealed class CondominiumRequest
{
    public string Cnpj { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public string? Phone { get; set; }
}

public sealed class CondominiumDto
{
    public Guid Id { get; set; }
    public string Cnpj { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
}

public sealed class ConformityItemDto
{
    public Guid Id { get; set; }
    public Guid CondominiumId { get; set; }
    public string Type { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public DateTime? DueDate { get; set; }
    public DateTime? CompletedAt { get; set; }
    public string? Notes { get; set; }
    public string Status { get; set; } = string.Empty;
}

public sealed class ConformityCreateRequest
{
    public string Description { get; set; } = string.Empty;
    public DateTime? DueDate { get; set; }
}

public sealed class ConformityCompleteRequest
{
    public string? Notes { get; set; }
}
