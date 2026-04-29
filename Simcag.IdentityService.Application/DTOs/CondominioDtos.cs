namespace Simcag.IdentityService.Application.DTOs;

/// <summary>Dados públicos de um condomínio — retornado sem autenticação em GET /api/condominios/lookup.</summary>
public sealed class CondominioLookupDto
{
    public Guid Id { get; set; }
    public string Nome { get; set; } = string.Empty;
    /// <summary>CNPJ formatado (XX.XXX.XXX/XXXX-XX).</summary>
    public string Cnpj { get; set; } = string.Empty;
}

public sealed class CondominioRequest
{
    public string Cnpj { get; set; } = string.Empty;
    public string Nome { get; set; } = string.Empty;
    public string Endereco { get; set; } = string.Empty;
    public string? Telefone { get; set; }
}

public sealed class CondominioDto
{
    public Guid Id { get; set; }
    public string Cnpj { get; set; } = string.Empty;
    public string Nome { get; set; } = string.Empty;
    public string Endereco { get; set; } = string.Empty;
    public string Telefone { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
}

public sealed class ConformityItemDto
{
    public Guid Id { get; set; }
    public Guid CondominioId { get; set; }
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
