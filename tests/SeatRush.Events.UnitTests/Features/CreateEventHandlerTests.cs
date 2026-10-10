using SeatRush.Events.Application.Features.CreateEvent;
using SeatRush.Events.Domain.Events;
using SeatRush.Events.UnitTests.Fakes;

namespace SeatRush.Events.UnitTests.Features;

public sealed class CreateEventHandlerTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 10, 12, 0, 0, TimeSpan.Zero);

    private readonly FakeEventRepository _repository = new();
    private readonly FakeUnitOfWork _unitOfWork = new();
    private readonly CreateEventHandler _handler;

    public CreateEventHandlerTests()
    {
        _handler = new CreateEventHandler(_repository, _unitOfWork, new FixedTimeProvider(Now));
    }

    [Fact]
    public async Task HandleAsync_adds_and_saves_the_event_and_returns_its_id()
    {
        var command = new CreateEventCommand("Rock Night", "Live music", Now.AddDays(30), Now.AddDays(30).AddHours(3));

        var result = await _handler.HandleAsync(command, TestContext.Current.CancellationToken);

        Assert.True(result.IsSuccess);
        var added = Assert.Single(_repository.Added);
        Assert.Equal(added.Id, result.Value);
        Assert.Equal("Rock Night", added.Title);
        Assert.Equal(1, _unitOfWork.SaveCount);
    }

    [Fact]
    public async Task HandleAsync_uses_the_time_provider_for_the_starts_in_future_rule()
    {
        var command = new CreateEventCommand("Rock Night", null, Now.AddMinutes(-1), Now.AddHours(3));

        var result = await _handler.HandleAsync(command, TestContext.Current.CancellationToken);

        Assert.Equal(EventErrors.StartsInPast, result.Error);
    }

    [Fact]
    public async Task HandleAsync_saves_nothing_when_the_domain_rejects_the_event()
    {
        var command = new CreateEventCommand("", null, Now.AddDays(30), Now.AddDays(30).AddHours(3));

        var result = await _handler.HandleAsync(command, TestContext.Current.CancellationToken);

        Assert.True(result.IsFailure);
        Assert.Empty(_repository.Added);
        Assert.Equal(0, _unitOfWork.SaveCount);
    }
}
