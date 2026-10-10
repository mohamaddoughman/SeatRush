using SeatRush.Events.Domain.Events;

namespace SeatRush.Events.UnitTests.Domain.Events;

public sealed class EventTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 10, 12, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset StartsAt = Now.AddDays(30);
    private static readonly DateTimeOffset EndsAt = StartsAt.AddHours(3);

    [Fact]
    public void Create_returns_a_draft_event_with_the_given_details()
    {
        var result = Event.Create("Rock Night", "Live music", StartsAt, EndsAt, Now);

        Assert.True(result.IsSuccess);
        var @event = result.Value;
        Assert.NotEqual(Guid.Empty, @event.Id);
        Assert.Equal("Rock Night", @event.Title);
        Assert.Equal("Live music", @event.Description);
        Assert.Equal(StartsAt, @event.StartsAt);
        Assert.Equal(EndsAt, @event.EndsAt);
        Assert.Equal(EventStatus.Draft, @event.Status);
    }

    [Fact]
    public void Create_allows_an_event_without_description()
    {
        var result = Event.Create("Rock Night", null, StartsAt, EndsAt, Now);

        Assert.True(result.IsSuccess);
        Assert.Null(result.Value.Description);
    }

    [Fact]
    public void Create_stores_times_in_utc()
    {
        var startsAt = new DateTimeOffset(2026, 11, 20, 20, 0, 0, TimeSpan.FromHours(3));

        var result = Event.Create("Rock Night", null, startsAt, startsAt.AddHours(3), Now);

        Assert.Equal(TimeSpan.Zero, result.Value.StartsAt.Offset);
        Assert.Equal(TimeSpan.Zero, result.Value.EndsAt.Offset);
        Assert.Equal(startsAt, result.Value.StartsAt);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_fails_when_title_is_empty(string title)
    {
        var result = Event.Create(title, null, StartsAt, EndsAt, Now);

        Assert.Equal(EventErrors.TitleRequired, result.Error);
    }

    [Fact]
    public void Create_fails_when_title_is_too_long()
    {
        var title = new string('a', Event.TitleMaxLength + 1);

        var result = Event.Create(title, null, StartsAt, EndsAt, Now);

        Assert.Equal(EventErrors.TitleTooLong, result.Error);
    }

    [Fact]
    public void Create_allows_a_title_at_the_maximum_length()
    {
        var title = new string('a', Event.TitleMaxLength);

        var result = Event.Create(title, null, StartsAt, EndsAt, Now);

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public void Create_trims_the_title()
    {
        var result = Event.Create("  Rock Night  ", null, StartsAt, EndsAt, Now);

        Assert.Equal("Rock Night", result.Value.Title);
    }

    [Fact]
    public void Create_checks_the_title_length_after_trimming()
    {
        var title = $"  {new string('a', Event.TitleMaxLength)}  ";

        var result = Event.Create(title, null, StartsAt, EndsAt, Now);

        Assert.True(result.IsSuccess);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_stores_a_blank_description_as_null(string description)
    {
        var result = Event.Create("Rock Night", description, StartsAt, EndsAt, Now);

        Assert.Null(result.Value.Description);
    }

    [Fact]
    public void Create_trims_the_description()
    {
        var result = Event.Create("Rock Night", "  Live music  ", StartsAt, EndsAt, Now);

        Assert.Equal("Live music", result.Value.Description);
    }

    [Fact]
    public void Create_checks_the_description_length_after_trimming()
    {
        var description = $"  {new string('a', Event.DescriptionMaxLength)}  ";

        var result = Event.Create("Rock Night", description, StartsAt, EndsAt, Now);

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public void Create_fails_when_description_is_too_long()
    {
        var description = new string('a', Event.DescriptionMaxLength + 1);

        var result = Event.Create("Rock Night", description, StartsAt, EndsAt, Now);

        Assert.Equal(EventErrors.DescriptionTooLong, result.Error);
    }

    [Fact]
    public void Create_fails_when_event_starts_in_the_past()
    {
        var startsAt = Now.AddMinutes(-1);

        var result = Event.Create("Rock Night", null, startsAt, startsAt.AddHours(3), Now);

        Assert.Equal(EventErrors.StartsInPast, result.Error);
    }

    [Fact]
    public void Create_fails_when_event_starts_exactly_now()
    {
        var result = Event.Create("Rock Night", null, Now, Now.AddHours(3), Now);

        Assert.Equal(EventErrors.StartsInPast, result.Error);
    }

    [Fact]
    public void Create_fails_when_event_ends_before_it_starts()
    {
        var result = Event.Create("Rock Night", null, StartsAt, StartsAt.AddMinutes(-1), Now);

        Assert.Equal(EventErrors.EndsBeforeStart, result.Error);
    }

    [Fact]
    public void Create_fails_when_event_ends_when_it_starts()
    {
        var result = Event.Create("Rock Night", null, StartsAt, StartsAt, Now);

        Assert.Equal(EventErrors.EndsBeforeStart, result.Error);
    }
}
