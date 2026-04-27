namespace Simcag.IdentityService.Infrastructure.Persistence;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Simcag.IdentityService.Infrastructure.Persistence.DbContext;

/// <summary>
/// Permite <c>dotnet ef migrations</c> sem subir a API (usa variável de ambiente ou connection string de desenvolvimento).
/// </summary>
public sealed class IdentityServiceDbContextFactory : IDesignTimeDbContextFactory<IdentityServiceDbContext>
{
    public IdentityServiceDbContext CreateDbContext(string[] args)
    {
        var cs =
            Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection")
            ?? Environment.GetEnvironmentVariable("CONNECTIONSTRINGS__DEFAULTCONNECTION")
            ?? "Host=localhost;Port=5432;Database=identity_db;Username=postgres;Password=postgres";

        var options = new DbContextOptionsBuilder<IdentityServiceDbContext>()
            .UseNpgsql(cs, o => o.CommandTimeout(30))
            .Options;

        return new IdentityServiceDbContext(options);
    }
}
