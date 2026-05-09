namespace Simcag.IdentityService.Application.UseCases.SetupAdmin;

using MediatR;

/// <summary>
/// Cria atomicamente um condomínio + o primeiro usuário ADMIN desse condomínio.
/// Só é permitido se o CNPJ ainda não estiver cadastrado no sistema.
/// </summary>
public sealed record SetupAdminCommand(
    // Condominium
    string Cnpj,
    string Name,
    string Address,
    string? Phone,
    // Dados do primeiro ADMIN
    string AdminEmail,
    string AdminPassword,
    string AdminName) : IRequest<SetupAdminResult>;

public sealed record SetupAdminResult(
    bool Success,
    string? Error,
    Guid? CondominiumId,
    Guid? UserId,
    string? AccessToken,
    string? RefreshToken,
    DateTime? AccessTokenExpiresAt);
