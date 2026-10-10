using SeatRush.Events.Application.Features.GetEventById;

namespace SeatRush.Events.Application.Abstractions;

/// <summary>
/// Read-side access to events: returns response shapes directly instead of entities.
/// </summary>
/// <remarks>
/// Why separate from <see cref="IEventRepository"/> (CQRS-lite): reads don't need the domain rules, so they skip
/// loading full entities and change tracking and fetch only the columns the response needs. Read handlers
/// also can't write by accident (interface segregation).
/// When not: in a small CRUD app where reads and writes look the same, one repository is enough.
/// </remarks>
public interface IEventQueries
{
    Task<EventResponse?> GetByIdAsync(Guid eventId, CancellationToken cancellationToken);
}
