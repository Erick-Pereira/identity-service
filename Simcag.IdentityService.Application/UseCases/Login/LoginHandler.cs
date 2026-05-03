namespace Simcag.IdentityService.Application.UseCases.Login;

using MediatR;
using Simcag.IdentityService.Application.Interfaces;
using Simcag.IdentityService.Domain.ValueObjects;
using Microsoft.Extensions.Logging;

public sealed class LoginCommandHandler : IRequestHandler<LoginCommand, LoginCommandResult>
{
    private readonly IUserRepository _userRepository;
    private readonly IJwtTokenService _jwtTokenService;
    private readonly IRefreshTokenRepository _refreshTokenRepository;
    private readonly ILogger<LoginCommandHandler> _logger;

    public LoginCommandHandler(
        IUserRepository userRepository,
        IJwtTokenService jwtTokenService,
        IRefreshTokenRepository refreshTokenRepository,
        ILogger<LoginCommandHandler> logger)
    {
        _userRepository = userRepository;
        _jwtTokenService = jwtTokenService;
        _refreshTokenRepository = refreshTokenRepository;
        _logger = logger;
    }

    public async Task<LoginCommandResult> Handle(LoginCommand request, CancellationToken ct)
    {
        _logger.LogInformation("Tentativa de login para email: {Email}, tenant: {TenantId}",
            request.Email, request.TenantId);

        try
        {
            // Validar email format
            var emailResult = Email.Create(request.Email);
            if (emailResult is Domain.Results.Result<Email>.Failure emailFail)
            {
                _logger.LogWarning("Email inválido: {Error}", emailFail.Error);
                return new LoginCommandResult(false, "Email ou senha inválidos", null, null, null, null);
            }

            var email = emailResult.Match(x => x, e => throw new InvalidOperationException());

            // Buscar usuário
            var user = await _userRepository.GetByEmailAndTenantAsync(
                email.Value, request.TenantId, ct);

            if (user == null || !user.IsActive)
            {
                _logger.LogWarning("Usuário não encontrado ou inativo: {Email}", email.Value);
                return new LoginCommandResult(false, "Email ou senha inválidos", null, null, null, null);
            }

            // Verificar senha
            if (!user.VerifyPassword(request.Password))
            {
                _logger.LogWarning("Senha incorreta para usuário: {UserId}", user.Id);
                return new LoginCommandResult(false, "Email ou senha inválidos", null, null, null, null);
            }

            // Gerar tokens
            var accessToken = await _jwtTokenService.GenerateAccessTokenAsync(
                user.Id, request.TenantId, user.Email.Value, user.Name, user.Role.Value, ct);

            var refreshToken = await _jwtTokenService.GenerateRefreshTokenAsync(
                user.Id, request.TenantId, ct);

            // Salvar refresh token
            var refreshTokenEntity = Domain.Entities.RefreshToken.Create(
                refreshToken,
                user.Id,
                request.TenantId,
                DateTime.UtcNow.AddDays(_jwtTokenService.RefreshTokenExpirationDays));

            if (refreshTokenEntity is Domain.Results.Result<Domain.Entities.RefreshToken>.Failure rtFail)
            {
                _logger.LogError("Erro ao criar refresh token: {Error}", rtFail.Error);
                return new LoginCommandResult(false, "Erro ao gerar tokens", null, null, null, null);
            }

            var refreshTokenValue = refreshTokenEntity.Match(x => x, e => throw new InvalidOperationException());
            await _refreshTokenRepository.AddAsync(refreshTokenValue, ct);

            _logger.LogInformation("Login bem-sucedido para usuário: {UserId}", user.Id);

            return new LoginCommandResult(
                true,
                null,
                user.Id,
                accessToken,
                refreshToken,
                DateTime.UtcNow.AddMinutes(_jwtTokenService.AccessTokenExpirationMinutes));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro durante login");
            return new LoginCommandResult(false, "Erro interno do servidor", null, null, null, null);
        }
    }
}
