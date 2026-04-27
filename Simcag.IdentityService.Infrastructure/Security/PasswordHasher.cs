namespace Simcag.IdentityService.Infrastructure.Security;

using Simcag.IdentityService.Application.Interfaces;
using BCrypt.Net;

public sealed class PasswordHasherService : IPasswordHasherService
{
    private const int WorkloadFactor = 12; // BCrypt work factor (custo computacional)

    public string HashPassword(string plainPassword)
    {
        if (string.IsNullOrWhiteSpace(plainPassword))
            throw new ArgumentException("Senha não pode estar vazia", nameof(plainPassword));

        return BCrypt.HashPassword(plainPassword, workFactor: WorkloadFactor);
    }

    public bool VerifyPassword(string plainPassword, string hash)
    {
        if (string.IsNullOrWhiteSpace(plainPassword) || string.IsNullOrWhiteSpace(hash))
            return false;

        try
        {
            return BCrypt.Verify(plainPassword, hash);
        }
        catch
        {
            return false;
        }
    }
}
