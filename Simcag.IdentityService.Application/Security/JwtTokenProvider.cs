using System.Security.Claims;
using Simcag.Shared.Security;

namespace Simcag.IdentityService.Application.Security;

/// <summary>
/// Fornece JWT com claims RBAC estruturados para cada perfil (Morador, Sindico/Conselho, Admin).
/// Implementa claims granulares e SoD constraints na camada de identidade.
/// </summary>
public static class JwtTokenProvider
{
    private const string TenantNameClaimType = "tenant_name";

    /// <summary>
    /// Gera JWT com claims RBAC completos baseado no perfil do usuário.
    /// </summary>
    public static List<Claim> GenerateClaims(
        string profile,
        IReadOnlyDictionary<string, string?> userData)
    {
        var baseClaims = new List<Claim>
        {
            new("sub", GetUserValue(userData, "userId") ?? "anonymous"),
            new("email", GetUserValue(userData, "email") ?? string.Empty),
            new("name", GetUserValue(userData, "fullName") ?? string.Empty),
            new(SimcagClaims.Role, MapProfileToRole(profile)),
            new(SimcagClaims.TenantId, GetUserValue(userData, "condoId") ?? string.Empty),
            new(TenantNameClaimType, GetUserValue(userData, "condoName") ?? string.Empty)
        };

        var profileSpecificClaims = GetProfileClaimsArray(profile);
        return [.. baseClaims, .. profileSpecificClaims];
    }

    private static string? GetUserValue(IReadOnlyDictionary<string, string?> data, string key) =>
        data.TryGetValue(key, out var value) ? value : null;

    private static string MapProfileToRole(string profile) => profile switch
    {
        "Admin" => SimcagRoles.Admin,
        "Sindico/Conselho" => SimcagRoles.Sindico,
        "Morador" => SimcagRoles.Morador,
        _ => profile.ToUpperInvariant()
    };

    private static List<Claim> GetProfileClaimsArray(string profile) => profile switch
    {
        "Morador" => MoradorClaims,
        "Sindico/Conselho" => SindicoConselhoClaims,
        "Admin" => AdminClaims,
        _ => []
    };

    private static List<Claim> MoradorClaims =>
    [
        new("permissions.dashboard.read:my-data-only", "true"),
        new("auditoria:view-report", "true"),
        new("insights:read", "true"),
        new("obrigacoes:read:own", "true"),
        new("compras:read:summary", "true"),
        new("alertas:view:directed", "true"),
        new(SimcagClaims.SodCanExecuteAudit, "false"),
        new(SimcagClaims.SodCanApproveOwnPurchase, "false"),
        new("permissions.auditoria.upload", "false"),
        new("permissions.suppliers.manage", "false"),
        new("permissions.products.manage", "false"),
    ];

    private static List<Claim> SindicoConselhoClaims =>
    [
        new("permissions.dashboard.read:full", "true"),
        new("auditoria:view-report", "true"),
        new("auditoria:view:detailed", "true"),
        new("compras:approve", "true"),
        new("compras:reject", "true"),
        new("obrigacoes:manage", "true"),
        new("permissions.suppliers.read", "true"),
        new("permissions.products.view", "true"),
        new("alertas:view:complete", "true"),
        new("notificacoes:push:read", "true"),
        new(SimcagClaims.SodCanExecuteAudit, "false"),
        new("permissions.auditoria.upload", "false"),
        new("compras:create", "false"),
    ];

    private static List<Claim> AdminClaims =>
    [
        new("permissions.dashboard.read:full", "true"),
        new("auditoria:view-report", "true"),
        new("auditoria:complete", "true"),
        new("auditoria:data-sources:read", "true"),
        new("uploads:create", "true"),
        new("ocr:process", "true"),
        new("compras:create", "true"),
        new("compras:edit", "true"),
        new("compras:read:detailed", "true"),
        new("permissions.suppliers.manage", "true"),
        new("fornecedores:create", "true"),
        new("fornecedores:update", "true"),
        new("fornecedores:delete", "true"),
        new("permissions.products.manage", "true"),
        new("produtos:create", "true"),
        new("produtos:update", "true"),
        new("produtos:delete", "true"),
        new("alertas:manage", "true"),
        new("notificacoes:templates:manage", "true"),
        new("notificacoes:issue", "true"),
        new("insights:read", "true"),
        new(SimcagClaims.SodCanApproveOwnPurchase, "false"),
    ];
}
