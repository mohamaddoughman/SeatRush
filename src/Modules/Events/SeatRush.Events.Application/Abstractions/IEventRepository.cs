using SeatRush.Events.Domain.Events;

namespace SeatRush.Events.Application.Abstractions;

/// <summary>
/// Write-side access to <see cref="Event"/> entities. Tracks changes but never saves them:
/// the handler calls <see cref="IUnitOfWork.SaveChangesAsync"/> once the whole use case is done.
/// </summary>
/// <remarks>
/// Only the methods a use case needs today. GetById, Remove and others arrive with the slices that use them.
/// </remarks>
public interface IEventRepository
{
    void Add(Event @event);
}
