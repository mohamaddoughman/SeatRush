using SeatRush.Events.Application.Abstractions;
using SeatRush.Events.Application.Features.GetEventById;
using SeatRush.Shared.Messaging;
using SeatRush.Shared.Results;

namespace SeatRush.Events.Application.Features.ListEvents;

/// <summary>
/// Always succeeds: an empty page is a valid answer, not an error (also for a page past the end).
/// </summary>
public sealed class ListEventsHandler(IEventQueries eventQueries)
    : IQueryHandler<ListEventsQuery, Result<PagedResponse<EventResponse>>>
{
    public async Task<Result<PagedResponse<EventResponse>>> HandleAsync(
        ListEventsQuery query,
        CancellationToken cancellationToken)
    {
        var page = await eventQueries.ListAsync(query, cancellationToken);

        return Result.Success(page);
    }
}
