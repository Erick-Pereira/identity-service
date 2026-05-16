namespace Simcag.IdentityService.Domain.Entities;

using Simcag.IdentityService.Domain.ValueObjects;
using Simcag.IdentityService.Domain.Results;
using Simcag.IdentityService.Domain.Events;

/// <summary>
/// Entidade de Usuário - Aggregate Root.
/// Representa um usuário do sistema, sempre pertencendo a um Tenant.
/// Encapsula validações e regras de negócio de identidade.
/// </summary>
public sealed class User : IAggregateRoot
{
    public Guid Id { get; private set; }
    /// <summary>Materializado pelo EF Core; o construtor sem parâmetros não atribui — use <see cref="Create"/>.</summary>
    public TenantId TenantId { get; private set; } = default!;
    public Email Email { get; private set; } = default!;
    public PasswordHash PasswordHash { get; private set; } = default!;
    public string Name { get; private set; } = default!;
    public Role Role { get; private set; } = default!;
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }
    public bool IsActive { get; private set; }

    private readonly List<IDomainEvent> _domainEvents = new();

    private User() { } // EF Core

    private User(TenantId tenantId, Email email, PasswordHash passwordHash, string name, Role role)
    {
        Id = Guid.NewGuid();
        TenantId = tenantId;
        Email = email;
        PasswordHash = passwordHash;
        Name = name.Trim();
        Role = role;
        CreatedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
        IsActive = true;
    }

    /// <summary>
    /// Factory method para criar um usuário com validação completa.
    /// </summary>
    public static Result<User> Create(
        Guid tenantId,
        string email,
        string passwordHash,
        string name,
        string role)
    {
        // Validar tenant
        var tenantIdResult = TenantId.Create(tenantId);
        if (tenantIdResult is Result<TenantId>.Failure f1)
            return Result<User>.Fail(f1.Error);

        // Validar email
        var emailResult = Email.Create(email);
        if (emailResult is Result<Email>.Failure f2)
            return Result<User>.Fail(f2.Error);

        // Validar password hash
        var passwordHashResult = PasswordHash.CreateFromHash(passwordHash);
        if (passwordHashResult is Result<PasswordHash>.Failure f3)
            return Result<User>.Fail(f3.Error);

        // Validar name
        if (string.IsNullOrWhiteSpace(name) || name.Length > 100)
            return Result<User>.Fail("Nome deve ter entre 1 e 100 caracteres");

        // Validar role
        var roleResult = Role.Create(role);
        if (roleResult is Result<Role>.Failure f4)
            return Result<User>.Fail(f4.Error);

        var user = new User(
            tenantIdResult.Match(x => x, e => throw new InvalidOperationException()),
            emailResult.Match(x => x, e => throw new InvalidOperationException()),
            passwordHashResult.Match(x => x, e => throw new InvalidOperationException()),
            name,
            roleResult.Match(x => x, e => throw new InvalidOperationException())
        );

        // Raiser evento de domínio
        user.AddDomainEvent(new UserRegisteredEvent(user.Id, user.TenantId.Value, user.Email.Value, user.Name));

        return Result<User>.Ok(user);
    }

    /// <summary>
    /// Atualiza o perfil do usuário.
    /// </summary>
    public Result UpdateProfile(string name)
    {
        if (string.IsNullOrWhiteSpace(name) || name.Length > 100)
            return Result.Fail("Nome deve ter entre 1 e 100 caracteres");

        if (Name == name.Trim())
            return Result.Ok();

        Name = name.Trim();
        UpdatedAt = DateTime.UtcNow;

        return Result.Ok();
    }

    /// <summary>
    /// Altera a senha do usuário.
    /// </summary>
    public Result ChangePassword(string newPasswordHash)
    {
        var passwordHashResult = PasswordHash.CreateFromHash(newPasswordHash);
        if (passwordHashResult is Result<PasswordHash>.Failure f)
            return Result.Fail(f.Error);

        PasswordHash = passwordHashResult.Match(x => x, e => throw new InvalidOperationException());
        UpdatedAt = DateTime.UtcNow;

        return Result.Ok();
    }

    /// <summary>
    /// Verifica se a senha está correta.
    /// </summary>
    public bool VerifyPassword(string plainPassword)
        => PasswordHash.VerifyPassword(plainPassword);

    /// <summary>
    /// Desativa o usuário.
    /// </summary>
    public void Deactivate()
    {
        IsActive = false;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Ativa o usuário.
    /// </summary>
    public void Activate()
    {
        IsActive = true;
        UpdatedAt = DateTime.UtcNow;
    }

    // Domain Events
    public IReadOnlyCollection<IDomainEvent> GetDomainEvents() => _domainEvents.AsReadOnly();
    public void ClearDomainEvents() => _domainEvents.Clear();
    private void AddDomainEvent(IDomainEvent @event) => _domainEvents.Add(@event);
}