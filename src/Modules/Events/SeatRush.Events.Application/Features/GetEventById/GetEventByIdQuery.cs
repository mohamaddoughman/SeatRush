using SeatRush.Shared.Messaging;
using SeatRush.Shared.Results;

namespace SeatRush.Events.Application.Features.GetEventById;

public sealed record GetEventByIdQuery(Guid EventId) : IQuery<Result<EventResponse>>;
