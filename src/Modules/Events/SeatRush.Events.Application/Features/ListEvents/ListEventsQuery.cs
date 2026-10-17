using SeatRush.Events.Application.Features.GetEventById;
using SeatRush.Events.Domain.Events;
using SeatRush.Shared.Messaging;
using SeatRush.Shared.Results;

namespace SeatRush.Events.Application.Features.ListEvents;

/// <summary>
/// One page of events, soonest first. All statuses unless <see cref="Status"/> narrows it; drafts are listed too
/// until Identity separates a public catalog from the organizer's view.
/// </summary>
/// <param name="Page">1-based page number.</param>
/// <param name="PageSize">Events per page, at most <see cref="MaxPageSize"/>.</param>
/// <param name="Status">Only events with this status; all statuses when null.</param>
/// <param name="StartsFrom">Only events starting at or after this instant (inclusive).</param>
/// <param name="StartsBefore">Only events starting before this instant (exclusive), so ranges like "all of May"
/// need no end-of-day arithmetic.</param>
public sealed record ListEventsQuery(
    int Page,
    int PageSize,
    EventStatus? Status,
    DateTimeOffset? StartsFrom,
    DateTimeOffset? StartsBefore) : IQuery<Result<PagedResponse<EventResponse>>>
{
    public const int DefaultPageSize = 20;

    public const int MaxPageSize = 100;

    /// <summary>Highest page number, so the row offset ((page - 1) × page size) can't overflow an int.</summary>
    public const int MaxPage = int.MaxValue / MaxPageSize;
}
