namespace SeatRush.Events.Application.Features.GetEventById;

/// <summary>
/// An event as the API returns it. <see cref="Status"/> is a string so clients don't depend on enum numbers.
/// </summary>
public sealed record EventResponse(
    Guid Id,
    string Title,
    string? Description,
    DateTimeOffset StartsAt,
    DateTimeOffset EndsAt,
    string Status);
