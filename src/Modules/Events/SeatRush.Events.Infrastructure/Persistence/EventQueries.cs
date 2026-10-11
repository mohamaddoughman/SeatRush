using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using SeatRush.Events.Application.Abstractions;
using SeatRush.Events.Application.Features.GetEventById;
using SeatRush.Events.Application.Features.ListEvents;
using SeatRush.Events.Domain.Events;

namespace SeatRush.Events.Infrastructure.Persistence;

/// <summary>
/// Read side: no change tracking, and the query selects only the columns the response needs.
/// </summary>
internal sealed class EventQueries(EventsDbContext dbContext) : IEventQueries
{
    // One mapping for every query that returns an event, so get-by-id and the list can't drift apart. An
    // expression (not a method) so EF Core translates it into the SELECT instead of loading whole entities.
    private static readonly Expression<Func<Event, EventResponse>> ToResponse = @event => new EventResponse(
        @event.Id,
        @event.Title,
        @event.Description,
        @event.StartsAt,
        @event.EndsAt,
        @event.Status.ToString());

    public Task<EventResponse?> GetByIdAsync(Guid eventId, CancellationToken cancellationToken) =>
        dbContext.Events
            .AsNoTracking()
            .Where(@event => @event.Id == eventId)
            .Select(ToResponse)
            .SingleOrDefaultAsync(cancellationToken);

    public async Task<PagedResponse<EventResponse>> ListAsync(ListEventsQuery query, CancellationToken cancellationToken)
    {
        var events = dbContext.Events.AsNoTracking();

        if (query.Status is { } status)
        {
            events = events.Where(@event => @event.Status == status);
        }

        if (query.StartsFrom is { } startsFrom)
        {
            events = events.Where(@event => @event.StartsAt >= startsFrom);
        }

        if (query.StartsBefore is { } startsBefore)
        {
            events = events.Where(@event => @event.StartsAt < startsBefore);
        }

        // Two round trips (count, then page): simple, and cheap at the size of an events list. They don't share a
        // snapshot, so a change in between can make the count differ from the page by a row (see PagedResponse).
        var totalCount = await events.CountAsync(cancellationToken);

        // Id breaks ties between events starting at the same time. Without it SQL Server may order them
        // differently on each query, and offset paging would show one twice and skip another.
        var items = await events
            .OrderBy(@event => @event.StartsAt)
            .ThenBy(@event => @event.Id)
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .Select(ToResponse)
            .ToListAsync(cancellationToken);

        return new PagedResponse<EventResponse>(items, query.Page, query.PageSize, totalCount);
    }
}
