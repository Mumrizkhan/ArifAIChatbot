namespace Shared.Domain.Common;

public abstract class AggregateRoot : AuditableEntity
{
    public int Version { get; set; } = 1;
}
