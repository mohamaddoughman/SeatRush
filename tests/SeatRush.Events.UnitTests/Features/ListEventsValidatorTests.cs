using SeatRush.Events.Application.Features.ListEvents;
using SeatRush.Events.Domain.Events;

namespace SeatRush.Events.UnitTests.Features;

public sealed class ListEventsValidatorTests
{
    private static readonly DateTimeOffset May = new(2030, 5, 1, 0, 0, 0, TimeSpan.Zero);

    private static readonly ListEventsQuery ValidQuery = new(1, ListEventsQuery.DefaultPageSize, null, null, null);

    private readonly ListEventsValidator _validator = new();

    [Fact]
    public void Accepts_the_defaults()
    {
        Assert.True(_validator.Validate(ValidQuery).IsValid);
    }

    [Fact]
    public void Accepts_every_filter_together()
    {
        var query = ValidQuery with { Status = EventStatus.Published, StartsFrom = May, StartsBefore = May.AddMonths(1) };

        Assert.True(_validator.Validate(query).IsValid);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(ListEventsQuery.MaxPage + 1)]
    public void Rejects_a_page_out_of_range(int page)
    {
        var result = _validator.Validate(ValidQuery with { Page = page });

        Assert.Contains(result.Errors, error => error.PropertyName == nameof(ListEventsQuery.Page));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(ListEventsQuery.MaxPageSize + 1)]
    public void Rejects_a_page_size_out_of_range(int pageSize)
    {
        var result = _validator.Validate(ValidQuery with { PageSize = pageSize });

        Assert.Contains(result.Errors, error => error.PropertyName == nameof(ListEventsQuery.PageSize));
    }

    [Fact]
    public void Accepts_the_maximum_page_size()
    {
        Assert.True(_validator.Validate(ValidQuery with { PageSize = ListEventsQuery.MaxPageSize }).IsValid);
    }

    [Fact]
    public void Rejects_an_undefined_status()
    {
        var result = _validator.Validate(ValidQuery with { Status = (EventStatus)7 });

        Assert.Contains(result.Errors, error => error.PropertyName == nameof(ListEventsQuery.Status));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Rejects_a_range_that_ends_at_or_before_its_start(int days)
    {
        var result = _validator.Validate(ValidQuery with { StartsFrom = May, StartsBefore = May.AddDays(days) });

        Assert.Contains(result.Errors, error => error.PropertyName == nameof(ListEventsQuery.StartsBefore));
    }
}
