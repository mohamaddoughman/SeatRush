using FluentValidation;
using SeatRush.Events.Domain.Events;

namespace SeatRush.Events.Application.Features.CreateEvent;

/// <summary>
/// Checks the shape of the request at the edge, so a client gets every problem at once, per field.
/// </summary>
/// <remarks>
/// The domain still enforces the same rules in <see cref="Event.Create"/>: the validator is for good error
/// messages, the domain is the guarantee. "Starts in the future" depends on the clock, so it stays in the domain only.
/// Lengths are checked on the trimmed text, because that's what <see cref="Event.Create"/> stores; otherwise a
/// padded title could pass here and then fail in the domain.
/// </remarks>
public sealed class CreateEventValidator : AbstractValidator<CreateEventCommand>
{
    public CreateEventValidator()
    {
        RuleFor(command => command.Title)
            .NotEmpty()
            .Must(title => FitsAfterTrimming(title, Event.TitleMaxLength))
            .WithMessage($"'{{PropertyName}}' must be {Event.TitleMaxLength} characters or fewer.");

        RuleFor(command => command.Description)
            .Must(description => FitsAfterTrimming(description, Event.DescriptionMaxLength))
            .WithMessage($"'{{PropertyName}}' must be {Event.DescriptionMaxLength} characters or fewer.");

        RuleFor(command => command.StartsAt)
            .NotEmpty();

        RuleFor(command => command.EndsAt)
            .NotEmpty()
            .GreaterThan(command => command.StartsAt)
            .WithMessage("'Ends At' must be after 'Starts At'.");
    }

    // Null passes: a missing title is reported by NotEmpty, and a missing description is allowed.
    private static bool FitsAfterTrimming(string? text, int maxLength) =>
        text is null || text.Trim().Length <= maxLength;
}
