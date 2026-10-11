namespace SeatRush.Events.Infrastructure.Api;

/// <summary>The body of a <c>201 Created</c> response to <c>POST /events</c>.</summary>
internal sealed record CreatedEventResponse(Guid Id);
