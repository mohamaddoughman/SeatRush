using SeatRush.Events.Application.Abstractions;
using SeatRush.Events.Application.Features.GetEventById;
using SeatRush.Events.Domain.Events;

namespace SeatRush.Events.UnitTests.Fakes;

// Hand-written fakes instead of a mocking library: the interfaces are tiny, and asserting on real state
// ("the repository now holds one event") reads more clearly than verifying calls.

internal sealed class FakeEventRepository : IEventRepository
{
    public List<Event> Added { get; } = [];

    public void Add(Event @event) => Added.Add(@event);
}

internal sealed class FakeUnitOfWork : IUnitOfWork
{
    public int SaveCount { get; private set; }

    public Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        SaveCount++;
        return Task.CompletedTask;
    }
}

internal sealed class FakeEventQueries(params EventResponse[] events) : IEventQueries
{
    public Task<EventResponse?> GetByIdAsync(Guid eventId, CancellationToken cancellationToken) =>
        Task.FromResult(events.SingleOrDefault(e => e.Id == eventId));
}

internal sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider
{
    public override DateTimeOffset GetUtcNow() => now;
}
