namespace Simcag.IdentityService.Infrastructure.Persistence.DbContext;

using Microsoft.EntityFrameworkCore;
using Simcag.IdentityService.Domain.Entities;
using Simcag.IdentityService.Domain.Results;
using Simcag.IdentityService.Domain.ValueObjects;

public class IdentityServiceDbContext : Microsoft.EntityFrameworkCore.DbContext
{
    private static TenantId TenantIdFromDb(Guid v) =>
        TenantId.Create(v) is Result<TenantId>.Success s ? s.Value
            : throw new InvalidOperationException("TenantId inválido lido do banco.");

    private static Email EmailFromDb(string v) =>
        Email.Create(v) is Result<Email>.Success s ? s.Value
            : throw new InvalidOperationException("Email inválido lido do banco.");

    private static PasswordHash PasswordHashFromDb(string v) =>
        PasswordHash.CreateFromHash(v) is Result<PasswordHash>.Success s ? s.Value
            : throw new InvalidOperationException("Hash inválido lido do banco.");

    private static Role RoleFromDb(string v) =>
        Role.Create(v) is Result<Role>.Success s ? s.Value
            : throw new InvalidOperationException("Role inválido lido do banco.");

    public IdentityServiceDbContext(DbContextOptions<IdentityServiceDbContext> options)
        : base(options)
    {
    }

    public DbSet<User> Users => Set<User>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // User Entity
        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(u => u.Id);

            // Value Object - TenantId
            entity.Property(u => u.TenantId)
                .HasConversion(v => v.Value, v => TenantIdFromDb(v))
                .IsRequired();

            // Value Object - Email
            entity.Property(u => u.Email)
                .HasConversion(v => v.Value, v => EmailFromDb(v))
                .HasMaxLength(254)
                .IsRequired();

            // Value Object - PasswordHash
            entity.Property(u => u.PasswordHash)
                .HasConversion(v => v.Value, v => PasswordHashFromDb(v))
                .HasMaxLength(256)
                .IsRequired();

            entity.Property(u => u.Name)
                .IsRequired()
                .HasMaxLength(100);

            // Value Object - Role
            entity.Property(u => u.Role)
                .HasConversion(v => v.Value, v => RoleFromDb(v))
                .IsRequired();

            entity.Property(u => u.CreatedAt)
                .IsRequired();

            entity.Property(u => u.UpdatedAt)
                .IsRequired();

            entity.Property(u => u.IsActive)
                .IsRequired();

            // Indexes
            entity.HasIndex(u => new { u.TenantId, u.Email })
                .IsUnique();

            entity.HasIndex(u => new { u.TenantId, u.Email, u.IsActive });
        });

        // RefreshToken Entity
        modelBuilder.Entity<RefreshToken>(entity =>
        {
            entity.HasKey(rt => rt.Id);

            entity.Property(rt => rt.Token)
                .IsRequired()
                .HasMaxLength(256);

            entity.Property(rt => rt.UserId)
                .IsRequired();

            // Value Object - TenantId
            entity.Property(rt => rt.TenantId)
                .HasConversion(v => v.Value, v => TenantIdFromDb(v))
                .IsRequired();

            entity.Property(rt => rt.ExpiresAt)
                .IsRequired();

            entity.Property(rt => rt.CreatedAt)
                .IsRequired();

            entity.Property(rt => rt.IsRevoked)
                .IsRequired();

            entity.Property(rt => rt.RevokedAt)
                .IsRequired(false);

            // Indexes
            entity.HasIndex(rt => rt.Token)
                .IsUnique();

            entity.HasIndex(rt => new { rt.UserId, rt.TenantId, rt.IsRevoked, rt.ExpiresAt });

            // Relationship with User
            entity.HasOne(rt => rt.User)
                .WithMany()
                .HasForeignKey(rt => rt.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}