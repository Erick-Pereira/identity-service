namespace Simcag.IdentityService.Application.Services;

using MediatR;
using Microsoft.Extensions.Logging;
using Simcag.IdentityService.Application.DTOs;
using Simcag.IdentityService.Application.Interfaces;
using Simcag.IdentityService.Application.UseCases.GetProfile;
using Simcag.IdentityService.Application.UseCases.Login;
using Simcag.IdentityService.Application.UseCases.RefreshToken;
using Simcag.IdentityService.Application.UseCases.Register;

public sealed class AuthService : IAuthService
{
    private readonly IMediator _mediator;
    private readonly IJwtTokenService _jwt;
    private readonly ILogger<AuthService> _logger;

    public AuthService(IMediator mediator, IJwtTokenService jwt, ILogger<AuthService> logger)
    {
        _mediator = mediator;
        _jwt = jwt;
        _logger = logger;
    }

    public async Task<AuthResult> RegisterAsync(RegisterRequest request, CancellationToken ct)
    {
        var cmd = new RegisterCommand(
            request.TenantId,
            request.Email,
            request.Password,
            request.Name,
            request.Role);

        var r = await _mediator.Send(cmd, ct);
        if (!r.Success)
            return AuthResult.Failure(r.Error ?? "Registro falhou");

        var profile = await _mediator.Send(new GetProfileQuery(r.UserId!.Value, request.TenantId), ct);
        if (profile is null)
            return AuthResult.Failure("Utilizador criado mas perfil indisponível");

        var expires = DateTime.UtcNow.AddMinutes(_jwt.AccessTokenExpirationMinutes);
        return AuthResult.FromCredentials(r.AccessToken!, r.RefreshToken!, expires, profile);
    }

    public async Task<AuthResult> LoginAsync(LoginRequest request, CancellationToken ct)
    {
        var r = await _mediator.Send(
            new LoginCommand(request.TenantId, request.Email, request.Password),
            ct);

        if (!r.Success || r.UserId is null)
            return AuthResult.Failure(r.Error ?? "Login falhou");

        var profile = await _mediator.Send(new GetProfileQuery(r.UserId.Value, request.TenantId), ct);
        if (profile is null)
            return AuthResult.Failure("Perfil indisponível");

        if (r.AccessToken is null || r.RefreshToken is null || r.ExpiresAt is null)
            return AuthResult.Failure("Tokens incompletos");

        return AuthResult.FromCredentials(r.AccessToken, r.RefreshToken, r.ExpiresAt.Value, profile);
    }

    public async Task<AuthResult> RefreshTokenAsync(RefreshTokenRequest request, CancellationToken ct)
    {
        var r = await _mediator.Send(new RefreshTokenCommand(request.RefreshToken), ct);
        if (!r.Success)
            return AuthResult.Failure(r.Error ?? "Refresh falhou");

        if (r.AccessToken is null || r.NewRefreshToken is null || r.ExpiresAt is null)
            return AuthResult.Failure("Tokens incompletos");

        var validation = await _jwt.ValidateTokenAsync(r.AccessToken, ct);
        if (!validation.IsValid || validation.UserId is null || validation.TenantId is null)
            return AuthResult.Failure("Access token inválido após refresh");

        var profile = await _mediator.Send(
            new GetProfileQuery(validation.UserId.Value, validation.TenantId.Value),
            ct);
        if (profile is null)
            return AuthResult.Failure("Perfil indisponível");

        return AuthResult.FromCredentials(r.AccessToken, r.NewRefreshToken, r.ExpiresAt.Value, profile);
    }

    public Task<UserProfileDto?> GetUserProfileAsync(Guid userId, Guid tenantId, CancellationToken ct) =>
        _mediator.Send(new GetProfileQuery(userId, tenantId), ct);

    public async Task<TokenValidationResponse> ValidateAccessTokenAsync(string authorizationHeader, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(authorizationHeader) ||
            !authorizationHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
        {
            return new TokenValidationResponse
            {
                IsValid = false,
                Error = "Token não fornecido"
            };
        }

        var token = authorizationHeader["Bearer ".Length..].Trim();
        if (string.IsNullOrEmpty(token))
        {
            return new TokenValidationResponse { IsValid = false, Error = "Token vazio" };
        }

        var r = await _jwt.ValidateTokenAsync(token, ct);
        if (!r.IsValid || r.UserId is null || r.TenantId is null)
        {
            return new TokenValidationResponse
            {
                IsValid = false,
                Error = r.Error ?? "Token inválido"
            };
        }

        var user = await _mediator.Send(new GetProfileQuery(r.UserId.Value, r.TenantId.Value), ct);
        if (user is null)
        {
            _logger.LogWarning("Validate: utilizador {UserId} não encontrado no tenant {TenantId}", r.UserId, r.TenantId);
            return new TokenValidationResponse
            {
                IsValid = false,
                Error = "Utilizador não encontrado"
            };
        }

        return new TokenValidationResponse
        {
            IsValid = true,
            UserId = user.Id.ToString(),
            TenantId = user.TenantId.ToString(),
            Role = user.Role
        };
    }
}
