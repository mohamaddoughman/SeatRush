using FluentValidation;

namespace SeatRush.Events.Application.Features.ListEvents;

/// <summary>
/// Checks the paging and filter values, so a client gets every problem at once instead of an empty or odd page.
/// </summary>
public sealed class ListEventsValidator : AbstractValidator<ListEventsQuery>
{
    public ListEventsValidator()
    {
        RuleFor(query => query.Page)
            .InclusiveBetween(1, ListEventsQuery.MaxPage);

        RuleFor(query => query.PageSize)
            .InclusiveBetween(1, ListEventsQuery.MaxPageSize);

        // The Api only binds defined names, but the query can come from other callers too: a cast like
        // (EventStatus)7 must not reach the database as a filter that silently matches nothing.
        RuleFor(query => query.Status)
            .IsInEnum();

        RuleFor(query => query.StartsBefore)
            .GreaterThan(query => query.StartsFrom)
            .When(query => query.StartsFrom is not null && query.StartsBefore is not null)
            .WithMessage("'Starts Before' must be after 'Starts From'.");
    }
}
