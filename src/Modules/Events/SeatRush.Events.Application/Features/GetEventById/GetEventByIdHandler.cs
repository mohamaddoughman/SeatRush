using SeatRush.Events.Application.Abstractions;
using SeatRush.Events.Domain.Events;
using SeatRush.Shared.Messaging;
using SeatRush.Shared.Results;

namespace SeatRush.Events.Application.Features.GetEventById;

public sealed class GetEventByIdHandler(IEventQueries eventQueries) : IQueryHandler<GetEventByIdQuery, Result<EventResponse>>
{
    public async Task<Result<EventResponse>> HandleAsync(GetEventByIdQuery query, CancellationToken cancellationToken)
    {
        var response = await eventQueries.GetByIdAsync(query.EventId, cancellationToken);

        return response is null
            ? Result.Failure<EventResponse>(EventErrors.NotFound(query.EventId))
            : Result.Success(response);
    }
}
