using Microsoft.EntityFrameworkCore;
using SeatRush.Events.Application.Abstractions;
using SeatRush.Events.Application.Features.GetEventById;

namespace SeatRush.Events.Infrastructure.Persistence;

/// <summary>
/// Read side: no change tracking, and the query selects only the columns the response needs.
/// </summary>
internal sealed class EventQueries(EventsDbContext dbContext) : IEventQueries
{
    public Task<EventResponse?> GetByIdAsync(Guid eventId, CancellationToken cancellationToken) =>
        dbContext.Events
            .AsNoTracking()
            .Where(@event => @event.Id == eventId)
            .Select(@event => new EventResponse(
                @event.Id,
                @event.Title,
                @event.Description,
                @event.StartsAt,
                @event.EndsAt,
                @event.Status.ToString()))
            .SingleOrDefaultAsync(cancellationToken);
}
