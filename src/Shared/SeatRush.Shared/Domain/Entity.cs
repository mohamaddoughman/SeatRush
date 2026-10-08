namespace SeatRush.Shared.Domain;

/// <summary>
/// Base class for entities in the rich domain models (Booking, Payments).
/// Collects domain events raised by behavior methods so they can be dispatched after the change is saved.
/// </summary>
/// <remarks>
/// Simple CRUD areas (e.g. parts of Events) don't need to inherit from this; a plain class is fine there.
/// </remarks>
public abstract class Entity<TId>
    where TId : notnull
{
    private readonly List<IDomainEvent> _domainEvents = [];

    protected Entity(TId id)
    {
        Id = id;
    }

    public TId Id { get; private init; }

    public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    public void ClearDomainEvents() => _domainEvents.Clear();

    protected void RaiseDomainEvent(IDomainEvent domainEvent) => _domainEvents.Add(domainEvent);
}
