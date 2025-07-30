using MediatR;

namespace Shared.Domain.Events;

public abstract class DomainEvent : INotification
{
    public DateTime OccurredOn { get; protected set; } = DateTime.UtcNow;
    public Guid TenantId { get; protected set; }
}
