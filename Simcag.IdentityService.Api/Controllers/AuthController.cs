namespace Simcag.IdentityService.Api.Controllers;

using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Simcag.IdentityService.Application.DTOs;
using Simcag.IdentityService.Application.UseCases.GetProfile;
using Simcag.IdentityService.Application.UseCases.Login;
using Simcag.IdentityService.Application.UseCases.Logout;
using Simcag.IdentityService.Application.UseCases.Register;
using Simcag.IdentityService.Application.UseCases.RefreshToken;
using Simcag.IdentityService.Application.UseCases.SetupAdmin;
using Simcag.IdentityService.Application.Interfaces;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Linq;

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public sealed class AuthController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly IJwtTokenService _jwtTokenService;
    private readonly ILogger<AuthController> _logger;

    public AuthController(IMediator mediator, IJwtTokenService jwtTokenService, ILogger<AuthController> logger)
    {
        _mediator = mediator;
        _jwtTokenService = jwtTokenService;
        _logger = logger;
    }

    /// <summary>
    /// Cria um condomínio e o seu primeiro usuário ADMIN em uma única chamada.
    /// Use este endpoint para registrar um novo condomínio no sistema.
    /// Falha se o CNPJ já estiver cadastrado (use /register para adicionar usuários a um condomínio existente).
    /// </summary>
    [HttpPost("setup")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(SetupResult), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Setup([FromBody] SetupRequest request, CancellationToken ct)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        _logger.LogInformation("Setup de condomínio solicitado — CNPJ: {Cnpj}", request.Cnpj);

        var command = new SetupAdminCommand(
            request.Cnpj, request.Nome, request.Endereco, request.Telefone,
            request.AdminEmail, request.AdminPassword, request.AdminName);

        var r = await _mediator.Send(command, ct);

        if (!r.Success)
        {
            _logger.LogWarning("Setup falhou: {Error}", r.Error);
            // CNPJ já cadastrado → 409 Conflict; outros erros → 400 Bad Request
            return r.Error!.Contains("já cadastrado", StringComparison.OrdinalIgnoreCase)
                ? Conflict(new { error = r.Error })
                : BadRequest(new { error = r.Error });
        }

        var result = new SetupResult
        {
            Success = true,
            CondominioId = r.CondominioId!.Value,
            AccessToken = r.AccessToken,
            RefreshToken = r.RefreshToken,
            ExpiresAt = r.AccessTokenExpiresAt,
            User = new UserProfileDto
            {
                Id = r.UserId!.Value,
                TenantId = r.CondominioId!.Value,
                Email = request.AdminEmail,
                Name = request.AdminName,
                Role = "ADMIN",
                CreatedAt = DateTime.UtcNow,
                IsActive = true
            }
        };

        return CreatedAtRoute("GetCurrentUserProfile", null, result);
    }

    /// <summary>Valida o access token (header Authorization: Bearer). Útil para gateway ou serviços que não validam JWT localmente.</summary>
    [HttpGet("validate")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(TokenValidationResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> ValidateAccessToken(CancellationToken ct)
    {
        var auth = Request.Headers.Authorization.ToString();
        if (string.IsNullOrWhiteSpace(auth) || !auth.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
            return Unauthorized(new { error = "Authorization Bearer obrigatório" });

        var token = auth["Bearer ".Length..].Trim();
        if (string.IsNullOrEmpty(token))
            return Unauthorized(new { error = "Token vazio" });

        var r = await _jwtTokenService.ValidateTokenAsync(token, ct);
        if (!r.IsValid || r.UserId is null)
            return Unauthorized(new { error = r.Error ?? "Token inválido" });

        var displayName = !string.IsNullOrWhiteSpace(r.Name) ? r.Name! : (r.Email ?? string.Empty);
        var body = new TokenValidationResponse
        {
            IsValid = true,
            UserId = r.UserId.Value.ToString(),
            TenantId = r.TenantId?.ToString() ?? string.Empty,
            UserName = displayName,
            Role = r.Role ?? string.Empty,
            Permissions = Array.Empty<string>(),
            ExpiresAt = r.ExpiresAtUtc ?? DateTime.UtcNow
        };

        return Ok(body);
    }

    /// <summary>
    /// Registra um novo usuário em um condomínio existente (TenantId obrigatório).
    /// Roles permitidos anonimamente: SINDICO, CONSELHO, MORADOR.
    /// Para registrar outro ADMIN, o chamador deve ser ADMIN autenticado do mesmo condomínio.
    /// Para criar o primeiro ADMIN de um novo condomínio, use POST /api/auth/setup.
    /// </summary>
    [HttpPost("register")]
    [ProducesResponseType(typeof(AuthResult), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Register(
        [FromBody] RegisterRequest request,
        CancellationToken ct)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        // Somente um Admin autenticado do mesmo tenant pode criar outro Admin.
        if (request.Role.Equals("Admin", StringComparison.OrdinalIgnoreCase))
        {
            var callerRole = User.FindFirstValue(ClaimTypes.Role)
                             ?? User.FindFirstValue("role");
            var callerTenantRaw = User.FindFirst("tenant_id")?.Value;

            if (!callerRole?.Equals("Admin", StringComparison.OrdinalIgnoreCase) == true
                || !Guid.TryParse(callerTenantRaw, out var callerTenant)
                || callerTenant != request.TenantId)
            {
                return StatusCode(StatusCodes.Status403Forbidden,
                    new { error = "Apenas um Admin autenticado do mesmo condomínio pode registrar outro Admin." });
            }
        }

        _logger.LogInformation("Requisição de registro recebida para tenant: {TenantId}, email: {Email}",
            request.TenantId, request.Email);

        var command = new RegisterCommand(
            request.TenantId,
            request.Email,
            request.Password,
            request.Name,
            request.Role);

        var result = await _mediator.Send(command, ct);

        if (!result.Success)
        {
            _logger.LogWarning("Erro no registro: {Error}", result.Error);
            return BadRequest(new { error = result.Error });
        }

        var dto = new UserProfileDto
        {
            Id = result.UserId!.Value,
            TenantId = request.TenantId,
            Email = request.Email,
            Name = request.Name,
            Role = request.Role,
            CreatedAt = result.UserCreatedAt ?? DateTime.UtcNow,
            IsActive = true
        };

        var authResult = new AuthResult
        {
            Success = true,
            AccessToken = result.AccessToken,
            RefreshToken = result.RefreshToken,
            ExpiresAt = result.AccessTokenExpiresAt,
            User = dto
        };

        return CreatedAtRoute("GetCurrentUserProfile", null, authResult);
    }

    /// <summary>
    /// Autentica um usuário e retorna tokens.
    /// </summary>
    /// <param name="request">Credenciais de login</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Tokens de acesso</returns>
    [HttpPost("login")]
    [ProducesResponseType(typeof(AuthResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Login(
        [FromBody] LoginRequest request,
        CancellationToken ct)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        _logger.LogInformation("Requisição de login recebida para tenant: {TenantId}, email: {Email}",
            request.TenantId, request.Email);

        var command = new LoginCommand(request.TenantId, request.Email, request.Password);
        var result = await _mediator.Send(command, ct);

        if (!result.Success)
        {
            _logger.LogWarning("Falha na autenticação: {Error}", result.Error);
            return Unauthorized(new { error = result.Error });
        }

        var authResult = new AuthResult
        {
            Success = true,
            AccessToken = result.AccessToken,
            RefreshToken = result.RefreshToken,
            ExpiresAt = result.ExpiresAt
        };

        return Ok(authResult);
    }

    /// <summary>
    /// Renova o access token usando um refresh token válido.
    /// </summary>
    [HttpPost("refresh")]
    [ProducesResponseType(typeof(AuthResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> RefreshToken(
        [FromBody] RefreshTokenRequest request,
        CancellationToken ct)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        _logger.LogInformation("Requisição de renovação de token recebida");

        var result = await _mediator.Send(new RefreshTokenCommand(request.RefreshToken), ct);
        if (!result.Success)
        {
            _logger.LogWarning("Refresh recusado: {Error}", result.Error);
            return Unauthorized(new { error = result.Error });
        }

        var authResult = new AuthResult
        {
            Success = true,
            AccessToken = result.AccessToken,
            RefreshToken = result.NewRefreshToken,
            ExpiresAt = result.ExpiresAt
        };

        return Ok(authResult);
    }

    /// <summary>
    /// Obtém o perfil do usuário autenticado.
    /// </summary>
    [HttpGet("profile", Name = "GetCurrentUserProfile")]
    [Authorize]
    [ProducesResponseType(typeof(UserProfileDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetProfile(CancellationToken ct)
    {
        var sub = User.FindFirst(JwtRegisteredClaimNames.Sub) ?? User.FindFirst(ClaimTypes.NameIdentifier);
        if (sub is null || !Guid.TryParse(sub.Value, out var userId))
            return Unauthorized(new { error = "Token inválido" });

        var tenantIdClaim = User.FindFirst("tenant_id");
        if (tenantIdClaim is null || !Guid.TryParse(tenantIdClaim.Value, out var tenantId))
            return Unauthorized(new { error = "Tenant não encontrado no token" });

        _logger.LogInformation("Perfil solicitado para utilizador: {UserId}", userId);

        var profile = await _mediator.Send(new GetProfileQuery(userId, tenantId), ct);
        if (profile is null)
            return NotFound(new { error = "Utilizador não encontrado" });

        return Ok(profile);
    }

    /// <summary>Encerra sessão: envie <c>refreshToken</c> no corpo para revogar no servidor; descarte sempre os tokens no cliente.</summary>
    [HttpPost("logout")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> Logout([FromBody] LogoutRequest? request, CancellationToken ct)
    {
        await _mediator.Send(new LogoutCommand(request?.RefreshToken), ct);
        return Ok(new { message = "Logout concluído. Descarte access e refresh no cliente." });
    }
}