using SeatRush.Events.Application.Features.GetEventById;
using SeatRush.Events.Application.Features.ListEvents;
using SeatRush.Events.Domain.Events;
using SeatRush.Events.UnitTests.Fakes;

namespace SeatRush.Events.UnitTests.Features;

public sealed class ListEventsHandlerTests
{
    private static readonly EventResponse RockNight = new(
        Guid.NewGuid(),
        "Rock Night",
        null,
        new DateTimeOffset(2026, 11, 20, 20, 0, 0, TimeSpan.Zero),
        new DateTimeOffset(2026, 11, 20, 23, 0, 0, TimeSpan.Zero),
        "Draft");

    [Fact]
    public async Task HandleAsync_returns_the_page_from_the_queries_for_the_given_query()
    {
        var queries = new FakeEventQueries(RockNight);
        var handler = new ListEventsHandler(queries);
        var query = new ListEventsQuery(2, 10, EventStatus.Draft, RockNight.StartsAt, null);

        var result = await handler.HandleAsync(query, TestContext.Current.CancellationToken);

        Assert.True(result.IsSuccess);
        Assert.Equal([RockNight], result.Value.Items);
        Assert.Equal(query, queries.LastListQuery);
    }

    [Fact]
    public async Task HandleAsync_returns_an_empty_page_as_success()
    {
        var handler = new ListEventsHandler(new FakeEventQueries());

        var result = await handler.HandleAsync(
            new ListEventsQuery(1, ListEventsQuery.DefaultPageSize, null, null, null),
            TestContext.Current.CancellationToken);

        Assert.True(result.IsSuccess);
        Assert.Empty(result.Value.Items);
    }
}
