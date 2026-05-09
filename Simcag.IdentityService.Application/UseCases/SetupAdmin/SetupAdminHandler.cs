namespace Simcag.IdentityService.Application.UseCases.SetupAdmin;

using MediatR;
using Microsoft.Extensions.Logging;
using Simcag.IdentityService.Application.Interfaces;
using Simcag.IdentityService.Domain.Entities;
using Simcag.IdentityService.Domain.Results;
using Simcag.IdentityService.Domain.ValueObjects;
using RoleVo = Simcag.IdentityService.Domain.ValueObjects.Role;

public sealed class SetupAdminHandler : IRequestHandler<SetupAdminCommand, SetupAdminResult>
{
    private readonly ICondominiumRepository _condominiumRepo;
    private readonly IUserRepository _userRepo;
    private readonly IRefreshTokenRepository _refreshTokenRepo;
    private readonly IPasswordHasherService _passwordHasher;
    private readonly IJwtTokenService _jwtTokenService;
    private readonly ILogger<SetupAdminHandler> _logger;

    public SetupAdminHandler(
        ICondominiumRepository condominiumRepo,
        IUserRepository userRepo,
        IRefreshTokenRepository refreshTokenRepo,
        IPasswordHasherService passwordHasher,
        IJwtTokenService jwtTokenService,
        ILogger<SetupAdminHandler> logger)
    {
        _condominiumRepo = condominiumRepo;
        _userRepo = userRepo;
        _refreshTokenRepo = refreshTokenRepo;
        _passwordHasher = passwordHasher;
        _jwtTokenService = jwtTokenService;
        _logger = logger;
    }

    public async Task<SetupAdminResult> Handle(SetupAdminCommand request, CancellationToken ct)
    {
        _logger.LogInformation("Setup solicitado para CNPJ: {Cnpj}, admin: {Email}", request.Cnpj, request.AdminEmail);

        try
        {
            // Verificar se o condomínio já existe (CNPJ já cadastrado)
            var existing = await _condominiumRepo.GetByCnpjAsync(NormalizeCnpj(request.Cnpj), ct);
            if (existing is not null)
                return Fail("CNPJ já cadastrado. Peça ao administrador do condomínio que crie sua conta.");

            // Validar e-mail do admin
            var emailResult = Email.Create(request.AdminEmail);
            if (emailResult is Result<Email>.Failure emailFail)
                return Fail(emailFail.Error);

            var email = emailResult.Match(x => x, _ => throw new InvalidOperationException());

            // Criar condomínio
            Condominium condo;
            try
            {
                condo = Condominium.Create(request.Cnpj, request.Name, request.Address, request.Phone);
            }
            catch (ArgumentException ex)
            {
                return Fail(ex.Message);
            }

            // Criar usuário ADMIN
            var passwordHash = _passwordHasher.HashPassword(request.AdminPassword);
            var userResult = User.Create(condo.Id, email.Value, passwordHash, request.AdminName, RoleVo.AdminValue);
            if (userResult is Result<User>.Failure userFail)
                return Fail(userFail.Error);

            var user = userResult.Match(x => x, _ => throw new InvalidOperationException());

            // Persistir condomínio e usuário
            await _condominiumRepo.AddAsync(condo, ct);
            await _userRepo.AddAsync(user, ct);

            // Gerar tokens
            var accessToken = await _jwtTokenService.GenerateAccessTokenAsync(
                user.Id, condo.Id, user.Email.Value, user.Name, user.Role.Value, ct);

            var refreshToken = await _jwtTokenService.GenerateRefreshTokenAsync(user.Id, condo.Id, ct);

            var rtResult = RefreshToken.Create(
                refreshToken,
                user.Id,
                condo.Id,
                DateTime.UtcNow.AddDays(_jwtTokenService.RefreshTokenExpirationDays));

            if (rtResult is Result<RefreshToken>.Failure rtFail)
                return Fail(rtFail.Error);

            await _refreshTokenRepo.AddAsync(rtResult.Match(x => x, _ => throw new InvalidOperationException()), ct);

            _logger.LogInformation("Setup complete — condominium: {CondominiumId}, admin: {UserId}", condo.Id, user.Id);

            return new SetupAdminResult(
                true, null,
                condo.Id, user.Id,
                accessToken, refreshToken,
                DateTime.UtcNow.AddMinutes(_jwtTokenService.AccessTokenExpirationMinutes));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro durante setup");
            return Fail("Erro interno do servidor");
        }
    }

    private static SetupAdminResult Fail(string? error) =>
        new(false, error, null, null, null, null, null);

    private static string NormalizeCnpj(string raw) =>
        new(raw.Where(char.IsDigit).ToArray());
}
