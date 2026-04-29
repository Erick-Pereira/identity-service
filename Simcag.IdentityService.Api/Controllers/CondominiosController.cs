namespace Simcag.IdentityService.Api.Controllers;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Simcag.IdentityService.Application.DTOs;
using Simcag.IdentityService.Application.Interfaces;
using Simcag.IdentityService.Domain.Entities;
using System.Security.Claims;
using System.Linq;

[ApiController]
[Route("api/condominios")]
[Authorize]
[Produces("application/json")]
public sealed class CondominiosController : ControllerBase
{
    private readonly ICondominioRepository _repo;
    private readonly ILogger<CondominiosController> _logger;

    public CondominiosController(ICondominioRepository repo, ILogger<CondominiosController> logger)
    {
        _repo = repo;
        _logger = logger;
    }

    private bool IsAdmin() => User.FindFirstValue(ClaimTypes.Role)?.Equals("Admin", StringComparison.OrdinalIgnoreCase) == true;

    private Guid? GetTenantId()
    {
        var raw = User.FindFirst("tenant_id")?.Value;
        return Guid.TryParse(raw, out var id) ? id : null;
    }

    /// <summary>
    /// Pesquisa pública de condomínios por nome ou CNPJ (sem autenticação).
    /// Retorna apenas dados públicos (id, nome, cnpj) para que o usuário descubra o TenantId
    /// antes de fazer login ou registro.
    /// </summary>
    [HttpGet("lookup")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(IEnumerable<CondominioLookupDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Lookup([FromQuery] string? q, CancellationToken ct)
    {
        var all = await _repo.ListAsync(ct);

        IEnumerable<Condominio> result = all.Where(c => c.IsActive);

        if (!string.IsNullOrWhiteSpace(q))
        {
            var term = q.Trim();
            var digits = new string(term.Where(char.IsDigit).ToArray());
            result = result.Where(c =>
                c.Nome.Contains(term, StringComparison.OrdinalIgnoreCase)
                || (digits.Length > 0 && c.Cnpj.Contains(digits)));
        }

        return Ok(result.Select(c => new CondominioLookupDto
        {
            Id   = c.Id,
            Nome = c.Nome,
            Cnpj = FormatCnpj(c.Cnpj)
        }));
    }

    private static string FormatCnpj(string digits) =>
        digits.Length == 14
            ? $"{digits[..2]}.{digits[2..5]}.{digits[5..8]}/{digits[8..12]}-{digits[12..14]}"
            : digits;

    [HttpPost]
    [ProducesResponseType(typeof(CondominioDto), StatusCodes.Status201Created)]
    public async Task<IActionResult> Create([FromBody] CondominioRequest req, CancellationToken ct)
    {
        if (!IsAdmin())
            return Forbid();

        var existing = await _repo.GetByCnpjAsync(req.Cnpj, ct);
        if (existing is not null)
            return Conflict(new { error = "CNPJ já cadastrado" });

        Condominio condo;
        try
        {
            condo = Condominio.Create(req.Cnpj, req.Nome, req.Endereco, req.Telefone);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { error = ex.Message });
        }

        await _repo.AddAsync(condo, ct);
        return CreatedAtAction(nameof(GetById), new { id = condo.Id }, ToDto(condo));
    }

    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<CondominioDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> List(CancellationToken ct)
    {
        if (IsAdmin())
        {
            var all = await _repo.ListAsync(ct);
            return Ok(all.Select(ToDto));
        }

        var tenantId = GetTenantId();
        if (tenantId is null) return Unauthorized();

        var c = await _repo.GetByIdAsync(tenantId.Value, ct);
        return c is null ? NotFound() : Ok(new[] { ToDto(c) });
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(CondominioDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        if (!IsAdmin())
        {
            var tenantId = GetTenantId();
            if (tenantId != id) return Forbid();
        }

        var c = await _repo.GetByIdAsync(id, ct);
        return c is null ? NotFound() : Ok(ToDto(c));
    }

    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(CondominioDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> Update(Guid id, [FromBody] CondominioRequest req, CancellationToken ct)
    {
        if (!IsAdmin())
        {
            var tenantId = GetTenantId();
            if (tenantId != id) return Forbid();
        }

        var c = await _repo.GetByIdAsync(id, ct);
        if (c is null) return NotFound();

        try
        {
            c.Update(req.Nome, req.Endereco, req.Telefone);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { error = ex.Message });
        }

        await _repo.UpdateAsync(c, ct);
        return Ok(ToDto(c));
    }

    [HttpGet("{id:guid}/conformities")]
    [ProducesResponseType(typeof(IEnumerable<ConformityItemDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> ListConformities(Guid id, CancellationToken ct)
    {
        if (!IsAdmin())
        {
            var tenantId = GetTenantId();
            if (tenantId != id) return Forbid();
        }

        var items = await _repo.ListConformitiesAsync(id, ct);
        return Ok(items.Select(ToDto));
    }

    [HttpPost("{id:guid}/conformities")]
    [ProducesResponseType(typeof(ConformityItemDto), StatusCodes.Status201Created)]
    public async Task<IActionResult> AddConformity(Guid id, [FromBody] ConformityCreateRequest req, CancellationToken ct)
    {
        if (!IsAdmin())
        {
            var tenantId = GetTenantId();
            if (tenantId != id) return Forbid();
        }

        var condo = await _repo.GetByIdAsync(id, ct);
        if (condo is null) return NotFound();

        ConformityItem item;
        try
        {
            item = condo.AddCustomConformity(req.Description, req.DueDate);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { error = ex.Message });
        }

        await _repo.UpdateAsync(condo, ct);
        return CreatedAtAction(nameof(ListConformities), new { id }, ToDto(item));
    }

    [HttpPost("{id:guid}/conformities/{itemId:guid}/complete")]
    [ProducesResponseType(typeof(ConformityItemDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> CompleteConformity(Guid id, Guid itemId, [FromBody] ConformityCompleteRequest? req, CancellationToken ct)
    {
        if (!IsAdmin())
        {
            var tenantId = GetTenantId();
            if (tenantId != id) return Forbid();
        }

        var item = await _repo.GetConformityAsync(id, itemId, ct);
        if (item is null) return NotFound();

        item.MarkCompleted(req?.Notes);
        await _repo.UpdateConformityAsync(item, ct);
        return Ok(ToDto(item));
    }

    [HttpPost("{id:guid}/conformities/{itemId:guid}/reopen")]
    public async Task<IActionResult> ReopenConformity(Guid id, Guid itemId, CancellationToken ct)
    {
        if (!IsAdmin())
        {
            var tenantId = GetTenantId();
            if (tenantId != id) return Forbid();
        }

        var item = await _repo.GetConformityAsync(id, itemId, ct);
        if (item is null) return NotFound();

        item.Reopen();
        await _repo.UpdateConformityAsync(item, ct);
        return Ok(ToDto(item));
    }

    private static CondominioDto ToDto(Condominio c) => new()
    {
        Id = c.Id,
        Cnpj = c.Cnpj,
        Nome = c.Nome,
        Endereco = c.Endereco,
        Telefone = c.Telefone,
        IsActive = c.IsActive,
        CreatedAt = c.CreatedAt
    };

    private static ConformityItemDto ToDto(ConformityItem item) => new()
    {
        Id = item.Id,
        CondominioId = item.CondominioId,
        Type = item.Type.ToString(),
        Description = item.Description,
        DueDate = item.DueDate,
        CompletedAt = item.CompletedAt,
        Notes = item.Notes,
        Status = item.Status.ToString().ToUpperInvariant()
    };
}
