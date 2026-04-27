namespace Simcag.IdentityService.Domain.Events;

public sealed record UserRegisteredEvent(
    Guid UserId,
    Guid TenantId,
    string Email,
    string Name) : IDomainEvent
{
    public Guid AggregateId => UserId;
    public DateTime OccurredAt { get; } = DateTime.UtcNow;
}
