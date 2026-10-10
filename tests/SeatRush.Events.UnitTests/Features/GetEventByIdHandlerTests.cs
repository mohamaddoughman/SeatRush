using SeatRush.Events.Application.Features.GetEventById;
using SeatRush.Events.Domain.Events;
using SeatRush.Events.UnitTests.Fakes;
using SeatRush.Shared.Results;

namespace SeatRush.Events.UnitTests.Features;

public sealed class GetEventByIdHandlerTests
{
    private static readonly EventResponse RockNight = new(
        Guid.NewGuid(),
        "Rock Night",
        null,
        new DateTimeOffset(2026, 11, 20, 20, 0, 0, TimeSpan.Zero),
        new DateTimeOffset(2026, 11, 20, 23, 0, 0, TimeSpan.Zero),
        "Draft");

    private readonly GetEventByIdHandler _handler = new(new FakeEventQueries(RockNight));

    [Fact]
    public async Task HandleAsync_returns_the_event_when_it_exists()
    {
        var result = await _handler.HandleAsync(new GetEventByIdQuery(RockNight.Id), TestContext.Current.CancellationToken);

        Assert.True(result.IsSuccess);
        Assert.Equal(RockNight, result.Value);
    }

    [Fact]
    public async Task HandleAsync_returns_not_found_when_the_event_does_not_exist()
    {
        var unknownId = Guid.NewGuid();

        var result = await _handler.HandleAsync(new GetEventByIdQuery(unknownId), TestContext.Current.CancellationToken);

        Assert.Equal(EventErrors.NotFound(unknownId), result.Error);
        Assert.Equal(ErrorType.NotFound, result.Error.Type);
    }
}
