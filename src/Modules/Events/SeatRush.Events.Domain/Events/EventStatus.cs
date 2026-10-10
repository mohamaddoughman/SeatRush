namespace SeatRush.Events.Domain.Events;

/// <summary>
/// Where an event is in its lifecycle: Draft → Published → Cancelled, or Draft → Cancelled.
/// </summary>
/// <remarks>
/// There is no "Completed" status: an event is over when <see cref="Event.EndsAt"/> has passed.
/// Storing it would need a background job to set it, and it could disagree with the dates.
/// The numbers are stored in the database: never renumber or reuse one; give a new status a new number.
/// </remarks>
public enum EventStatus
{
    /// <summary>Created by an admin, not visible to customers yet.</summary>
    Draft = 0,

    /// <summary>Visible to customers and bookable.</summary>
    Published = 1,

    /// <summary>Final: a cancelled event can't be reopened.</summary>
    Cancelled = 2,
}
