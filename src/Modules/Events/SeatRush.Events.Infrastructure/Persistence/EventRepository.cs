using SeatRush.Events.Application.Abstractions;
using SeatRush.Events.Domain.Events;

namespace SeatRush.Events.Infrastructure.Persistence;

internal sealed class EventRepository(EventsDbContext dbContext) : IEventRepository
{
    public void Add(Event @event) => dbContext.Events.Add(@event);
}
