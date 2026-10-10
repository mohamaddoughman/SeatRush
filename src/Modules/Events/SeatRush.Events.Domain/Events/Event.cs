using SeatRush.Shared.Results;

namespace SeatRush.Events.Domain.Events;

/// <summary>
/// Something customers can buy tickets for: a concert, a match, a conference.
/// </summary>
/// <remarks>
/// Events is mostly CRUD, so this is a plain class rather than an <c>Entity&lt;TId&gt;</c> with domain events.
/// It still guards its own rules (valid dates, status transitions) through a factory method and private setters,
/// because an invalid event should be impossible to create no matter who calls it (API, seeder, tests).
/// </remarks>
public sealed class Event
{
    // Public so the validator and the EF Core configuration use the same limits as the domain.
    public const int TitleMaxLength = 200;
    public const int DescriptionMaxLength = 2000;

    // Also used by EF Core to materialize rows: it binds constructor parameters to properties by name.
    private Event(Guid id, string title, string? description, DateTimeOffset startsAt, DateTimeOffset endsAt, EventStatus status)
    {
        Id = id;
        Title = title;
        Description = description;
        StartsAt = startsAt;
        EndsAt = endsAt;
        Status = status;
    }

    public Guid Id { get; private set; }

    public string Title { get; private set; }

    public string? Description { get; private set; }

    /// <summary>When the event starts, in UTC.</summary>
    public DateTimeOffset StartsAt { get; private set; }

    /// <summary>When the event ends, in UTC.</summary>
    public DateTimeOffset EndsAt { get; private set; }

    public EventStatus Status { get; private set; }

    /// <summary>
    /// Creates a new event as a <see cref="EventStatus.Draft"/>. Times are stored in UTC.
    /// The title is trimmed, and a blank description becomes <c>null</c>, before the rules are checked.
    /// <paramref name="now"/> is passed in rather than read from the clock, so the "starts in the future"
    /// rule is testable.
    /// </summary>
    public static Result<Event> Create(
        string title,
        string? description,
        DateTimeOffset startsAt,
        DateTimeOffset endsAt,
        DateTimeOffset now)
    {
        if (string.IsNullOrWhiteSpace(title))
        {
            return Result.Failure<Event>(EventErrors.TitleRequired);
        }

        // Normalized here, not in the API, so every caller stores text the same way:
        // no padded titles, and "no description" is always null, never "".
        title = title.Trim();
        description = string.IsNullOrWhiteSpace(description) ? null : description.Trim();

        if (title.Length > TitleMaxLength)
        {
            return Result.Failure<Event>(EventErrors.TitleTooLong);
        }

        if (description?.Length > DescriptionMaxLength)
        {
            return Result.Failure<Event>(EventErrors.DescriptionTooLong);
        }

        if (startsAt <= now)
        {
            return Result.Failure<Event>(EventErrors.StartsInPast);
        }

        if (endsAt <= startsAt)
        {
            return Result.Failure<Event>(EventErrors.EndsBeforeStart);
        }

        var @event = new Event(
            Guid.NewGuid(),
            title,
            description,
            startsAt.ToUniversalTime(),
            endsAt.ToUniversalTime(),
            EventStatus.Draft);

        return Result.Success(@event);
    }
}
