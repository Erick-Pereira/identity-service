using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Simcag.IdentityService.Domain.Entities;
using Simcag.IdentityService.Domain.ValueObjects;

namespace Simcag.IdentityService.Infrastructure.Persistence.DbContext;

public class IdentityServiceDbContext : Microsoft.EntityFrameworkCore.DbContext
{
    public IdentityServiceDbContext(DbContextOptions<IdentityServiceDbContext> options)
        : base(options)
    {
    }

    public DbSet<User> Users => Set<User>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<Condominio> Condominios => Set<Condominio>();
    public DbSet<ConformityItem> ConformityItems => Set<ConformityItem>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Condominio>(entity =>
        {
            entity.HasKey(c => c.Id);

            entity.Property(c => c.Cnpj)
                .IsRequired()
                .HasMaxLength(14);

            entity.Property(c => c.Nome)
                .IsRequired()
                .HasMaxLength(200);

            entity.Property(c => c.Endereco)
                .IsRequired()
                .HasMaxLength(300);

            entity.Property(c => c.Telefone)
                .IsRequired()
                .HasMaxLength(40);

            entity.Property(c => c.IsActive).IsRequired();
            entity.Property(c => c.CreatedAt).IsRequired();
            entity.Property(c => c.UpdatedAt).IsRequired();

            entity.HasIndex(c => c.Cnpj).IsUnique();

            entity.HasMany(c => c.Conformities)
                .WithOne()
                .HasForeignKey(ci => ci.CondominioId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<ConformityItem>(entity =>
        {
            entity.HasKey(c => c.Id);

            entity.Property(c => c.Type)
                .IsRequired()
                .HasConversion<int>();

            entity.Property(c => c.Description)
                .IsRequired()
                .HasMaxLength(500);

            entity.Property(c => c.DueDate);
            entity.Property(c => c.CompletedAt);
            entity.Property(c => c.Notes).HasMaxLength(1000);
            entity.Property(c => c.CreatedAt).IsRequired();
            entity.Property(c => c.UpdatedAt).IsRequired();

            entity.HasIndex(c => new { c.CondominioId, c.Type });
        });

        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(u => u.Id);

            entity.Property(u => u.TenantId)
                .HasConversion(new ValueConverter<TenantId, Guid>(v => v.Value, v => TenantId.FromStorage(v)));

            entity.Property(u => u.Email)
                .HasConversion(new ValueConverter<Email, string>(v => v.Value, v => Email.FromStorage(v)))
                .IsRequired()
                .HasMaxLength(254);

            entity.Property(u => u.PasswordHash)
                .HasConversion(new ValueConverter<PasswordHash, string>(v => v.Value, v => PasswordHash.FromStorage(v)))
                .IsRequired()
                .HasMaxLength(256);

            entity.Property(u => u.Name)
                .IsRequired()
                .HasMaxLength(100);

            entity.Property(u => u.Role)
                .IsRequired()
                .HasConversion(new ValueConverter<Role, string>(r => r.Value, v => Role.FromStorage(v)));

            entity.Property(u => u.CreatedAt)
                .IsRequired();

            entity.Property(u => u.UpdatedAt)
                .IsRequired();

            entity.Property(u => u.IsActive)
                .IsRequired();

            entity.HasIndex(u => new { u.TenantId, u.Email }).IsUnique();
            entity.HasIndex(u => new { u.TenantId, u.Email, u.IsActive });
        });

        modelBuilder.Entity<RefreshToken>(entity =>
        {
            entity.HasKey(rt => rt.Id);

            entity.Property(rt => rt.TenantId)
                .HasConversion(new ValueConverter<TenantId, Guid>(v => v.Value, v => TenantId.FromStorage(v)));

            entity.Property(rt => rt.Token)
                .IsRequired()
                .HasMaxLength(256);

            entity.Property(rt => rt.UserId)
                .IsRequired();

            entity.Property(rt => rt.ExpiresAt)
                .IsRequired();

            entity.Property(rt => rt.CreatedAt)
                .IsRequired();

            entity.Property(rt => rt.IsRevoked)
                .IsRequired();

            entity.Property(rt => rt.RevokedAt)
                .IsRequired(false);

            entity.HasIndex(rt => rt.Token)
                .IsUnique();

            entity.HasIndex(rt => new { rt.UserId, rt.TenantId, rt.IsRevoked, rt.ExpiresAt });

            // Relationship with User
            entity.HasOne(rt => rt.User)
                .WithMany() // User doesn't have navigation to RefreshTokens
                .HasForeignKey(rt => rt.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}