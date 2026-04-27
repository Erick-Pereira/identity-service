namespace Simcag.IdentityService.Domain.Events;

/// <summary>
/// Interface para eventos de domínio.
/// </summary>
public interface IDomainEvent
{
    Guid AggregateId { get; }
    DateTime OccurredAt { get; }
}
