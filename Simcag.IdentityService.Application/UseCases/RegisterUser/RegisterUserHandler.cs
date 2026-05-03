namespace Simcag.IdentityService.Application.UseCases.Register;

using MediatR;
using Simcag.IdentityService.Application.Interfaces;
using Simcag.IdentityService.Domain.Entities;
using Simcag.IdentityService.Domain.ValueObjects;
using Microsoft.Extensions.Logging;

public sealed class RegisterCommandHandler : IRequestHandler<RegisterCommand, RegisterCommandResult>
{
    private readonly IUserRepository _userRepository;
    private readonly IPasswordHasherService _passwordHasher;
    private readonly IJwtTokenService _jwtTokenService;
    private readonly IRefreshTokenRepository _refreshTokenRepository;
    private readonly ILogger<RegisterCommandHandler> _logger;

    public RegisterCommandHandler(
        IUserRepository userRepository,
        IPasswordHasherService passwordHasher,
        IJwtTokenService jwtTokenService,
        IRefreshTokenRepository refreshTokenRepository,
        ILogger<RegisterCommandHandler> logger)
    {
        _userRepository = userRepository;
        _passwordHasher = passwordHasher;
        _jwtTokenService = jwtTokenService;
        _refreshTokenRepository = refreshTokenRepository;
        _logger = logger;
    }

    public async Task<RegisterCommandResult> Handle(RegisterCommand request, CancellationToken ct)
    {
        _logger.LogInformation("Tentativa de registro para email: {Email}, tenant: {TenantId}",
            request.Email, request.TenantId);

        try
        {
            // Validar email
            var emailResult = Email.Create(request.Email);
            if (emailResult is Domain.Results.Result<Email>.Failure emailFail)
            {
                _logger.LogWarning("Email inválido: {Error}", emailFail.Error);
                return new RegisterCommandResult(false, emailFail.Error, null, null, null);
            }

            var email = emailResult.Match(x => x, e => throw new InvalidOperationException());

            // Verificar se usuário já existe
            var existingUser = await _userRepository.GetByEmailAndTenantAsync(
                email.Value, request.TenantId, ct);

            if (existingUser != null)
            {
                _logger.LogWarning("Usuário já existe: {Email}", email.Value);
                return new RegisterCommandResult(false, "Usuário já existe", null, null, null);
            }

            // Hash da senha
            var passwordHashValue = _passwordHasher.HashPassword(request.Password);

            // Criar usuário
            var userResult = User.Create(
                request.TenantId,
                email.Value,
                passwordHashValue,
                request.Name,
                request.Role);

            if (userResult is Domain.Results.Result<User>.Failure userFail)
            {
                _logger.LogWarning("Erro ao criar usuário: {Error}", userFail.Error);
                return new RegisterCommandResult(false, userFail.Error, null, null, null);
            }

            var user = userResult.Match(x => x, e => throw new InvalidOperationException());

            // Persistir usuário
            await _userRepository.AddAsync(user, ct);

            // Gerar tokens
            var accessToken = await _jwtTokenService.GenerateAccessTokenAsync(
                user.Id, request.TenantId, user.Email.Value, user.Name, user.Role.Value, ct);

            var refreshToken = await _jwtTokenService.GenerateRefreshTokenAsync(
                user.Id, request.TenantId, ct);

            // Salvar refresh token
            var refreshTokenEntity = RefreshToken.Create(
                refreshToken,
                user.Id,
                request.TenantId,
                DateTime.UtcNow.AddDays(_jwtTokenService.RefreshTokenExpirationDays));

            if (refreshTokenEntity is Domain.Results.Result<RefreshToken>.Failure rtFail)
            {
                _logger.LogError("Erro ao criar refresh token: {Error}", rtFail.Error);
                return new RegisterCommandResult(false, "Erro ao gerar refresh token", null, null, null);
            }

            var refreshTokenValue = refreshTokenEntity.Match(
                v => v,
                e => throw new InvalidOperationException(e));
            await _refreshTokenRepository.AddAsync(refreshTokenValue, ct);

            _logger.LogInformation("Usuário registrado com sucesso: {UserId}", user.Id);

            return new RegisterCommandResult(
                true,
                null,
                user.Id,
                accessToken,
                refreshToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro durante registro");
            return new RegisterCommandResult(false, "Erro interno do servidor", null, null, null);
        }
    }
}
