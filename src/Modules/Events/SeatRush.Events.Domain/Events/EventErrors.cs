using SeatRush.Shared.Results;

namespace SeatRush.Events.Domain.Events;

/// <summary>
/// The expected failures of the Events module, in one place so codes stay unique and stable.
/// </summary>
public static class EventErrors
{
    public static readonly Error TitleRequired = new(
        "Events.TitleRequired", "The title is required.", ErrorType.Validation);

    public static readonly Error TitleTooLong = new(
        "Events.TitleTooLong", $"The title can't be longer than {Event.TitleMaxLength} characters.", ErrorType.Validation);

    public static readonly Error DescriptionTooLong = new(
        "Events.DescriptionTooLong", $"The description can't be longer than {Event.DescriptionMaxLength} characters.", ErrorType.Validation);

    public static readonly Error StartsInPast = new(
        "Events.StartsInPast", "The event must start in the future.", ErrorType.Validation);

    public static readonly Error EndsBeforeStart = new(
        "Events.EndsBeforeStart", "The event must end after it starts.", ErrorType.Validation);

    public static Error NotFound(Guid eventId) => new(
        "Events.NotFound", $"No event with id '{eventId}' was found.", ErrorType.NotFound);
}
