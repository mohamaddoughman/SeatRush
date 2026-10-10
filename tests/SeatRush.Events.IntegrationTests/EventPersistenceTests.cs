using Microsoft.Extensions.DependencyInjection;
using SeatRush.Events.Application.Abstractions;
using SeatRush.Events.Application.Features.CreateEvent;
using SeatRush.Events.Application.Features.GetEventById;
using SeatRush.Events.Domain.Events;
using SeatRush.Shared.Results;

namespace SeatRush.Events.IntegrationTests;

/// <summary>
/// The real handlers against the real repository, queries and database. Each step runs in its own DI scope,
/// like separate HTTP requests, so a read can't be answered from the DbContext's memory instead of SQL Server.
/// </summary>
public sealed class EventPersistenceTests(EventsDatabaseFixture database)
{
    [Fact]
    public async Task A_created_event_can_be_read_back_by_id()
    {
        var startsAt = new DateTimeOffset(2030, 5, 1, 20, 0, 0, TimeSpan.FromHours(3));
        var command = new CreateEventCommand("Rock Night", "Live music", startsAt, startsAt.AddHours(3));

        var created = await CreateAsync(command);
        var result = await GetByIdAsync(created);

        Assert.True(result.IsSuccess);
        Assert.Equal(
            new EventResponse(created, "Rock Night", "Live music", startsAt, startsAt.AddHours(3), nameof(EventStatus.Draft)),
            result.Value);
    }

    [Fact]
    public async Task Times_are_read_back_in_utc()
    {
        var startsAt = new DateTimeOffset(2030, 5, 1, 20, 0, 0, TimeSpan.FromHours(3));

        var created = await CreateAsync(new CreateEventCommand("Rock Night", null, startsAt, startsAt.AddHours(3)));
        var result = await GetByIdAsync(created);

        Assert.Equal(TimeSpan.Zero, result.Value.StartsAt.Offset);
        Assert.Equal(TimeSpan.Zero, result.Value.EndsAt.Offset);
    }

    [Fact]
    public async Task A_missing_description_is_read_back_as_null()
    {
        var startsAt = new DateTimeOffset(2030, 5, 1, 20, 0, 0, TimeSpan.Zero);

        var created = await CreateAsync(new CreateEventCommand("Rock Night", "   ", startsAt, startsAt.AddHours(3)));
        var result = await GetByIdAsync(created);

        Assert.Null(result.Value.Description);
    }

    [Fact]
    public async Task Reading_an_unknown_id_returns_not_found()
    {
        var unknownId = Guid.NewGuid();

        var result = await GetByIdAsync(unknownId);

        Assert.Equal(EventErrors.NotFound(unknownId), result.Error);
    }

    private async Task<Guid> CreateAsync(CreateEventCommand command)
    {
        await using var scope = database.Services.CreateAsyncScope();
        var handler = new CreateEventHandler(
            scope.ServiceProvider.GetRequiredService<IEventRepository>(),
            scope.ServiceProvider.GetRequiredService<IUnitOfWork>(),
            TimeProvider.System);

        var result = await handler.HandleAsync(command, TestContext.Current.CancellationToken);

        Assert.True(result.IsSuccess);
        return result.Value;
    }

    private async Task<Result<EventResponse>> GetByIdAsync(Guid eventId)
    {
        await using var scope = database.Services.CreateAsyncScope();
        var handler = new GetEventByIdHandler(scope.ServiceProvider.GetRequiredService<IEventQueries>());

        return await handler.HandleAsync(new GetEventByIdQuery(eventId), TestContext.Current.CancellationToken);
    }
}
