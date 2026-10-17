namespace SeatRush.Events.Application.Features.ListEvents;

/// <summary>One page of a list, with what a client needs to show "page 2 of 7".</summary>
/// <remarks>
/// <see cref="TotalCount"/> is counted just before the page is read, so it can be off by a row when events are
/// added or removed in between: fine for showing "page 2 of 7", not an exact figure.
/// In the Events module while it's the only list. When a second module pages, it moves to Shared: every list in
/// the API should have the same page shape.
/// </remarks>
public sealed record PagedResponse<T>(IReadOnlyList<T> Items, int Page, int PageSize, int TotalCount)
{
    // Guarded here, not only by the validator: TotalPages divides by it, and a page can be built by any caller.
    public int PageSize { get; } = PageSize > 0
        ? PageSize
        : throw new ArgumentOutOfRangeException(nameof(PageSize), PageSize, "Page size must be positive.");

    public int TotalPages => (int)Math.Ceiling(TotalCount / (double)PageSize);
}
