using SeatRush.Shared.Messaging;
using SeatRush.Shared.Results;

namespace SeatRush.Events.Application.Features.CreateEvent;

/// <summary>
/// Creates a new event as a draft. Returns the new event's id.
/// </summary>
public sealed record CreateEventCommand(
    string Title,
    string? Description,
    DateTimeOffset StartsAt,
    DateTimeOffset EndsAt) : ICommand<Result<Guid>>;
