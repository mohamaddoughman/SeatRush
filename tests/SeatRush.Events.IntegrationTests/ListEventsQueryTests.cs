using System.Data.SqlTypes;
using Microsoft.Extensions.DependencyInjection;
using SeatRush.Events.Application.Abstractions;
using SeatRush.Events.Application.Features.GetEventById;
using SeatRush.Events.Application.Features.ListEvents;
using SeatRush.Events.Domain.Events;

namespace SeatRush.Events.IntegrationTests;

/// <summary>
/// Sorting, paging and filtering of the real list query in SQL Server.
/// </summary>
/// <remarks>Each test uses a fresh database of its own, because it checks every row the list returns.</remarks>
public sealed class ListEventsQueryTests(EventsDatabaseFixture fixture)
{
    private static readonly DateTimeOffset May = new(2030, 5, 1, 20, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task Lists_soonest_first_with_ties_broken_by_id()
    {
        await using var database = await fixture.CreateFreshDatabaseAsync(TestContext.Current.CancellationToken);
        var later = await AddAsync(database, "Later", May.AddDays(1));
        var tieA = await AddAsync(database, "Tie A", May);
        var tieB = await AddAsync(database, "Tie B", May);

        var page = await ListAsync(database, Query());

        Guid[] tie = [tieA, tieB];
        // SqlGuid sorts like SQL Server (last bytes first), unlike .NET's own Guid ordering.
        Assert.Equal([.. tie.OrderBy(id => new SqlGuid(id)), later], page.Items.Select(item => item.Id));
    }

    [Fact]
    public async Task Pages_through_the_list_with_the_total_count()
    {
        await using var database = await fixture.CreateFreshDatabaseAsync(TestContext.Current.CancellationToken);
        var ids = new List<Guid>();
        for (var day = 0; day < 5; day++)
        {
            ids.Add(await AddAsync(database, $"Day {day}", May.AddDays(day)));
        }

        var page = await ListAsync(database, Query() with { Page = 2, PageSize = 2 });

        Assert.Equal(ids[2..4], page.Items.Select(item => item.Id));
        Assert.Equal(5, page.TotalCount);
        Assert.Equal(3, page.TotalPages);
    }

    [Fact]
    public async Task A_page_past_the_end_is_empty_but_keeps_the_total_count()
    {
        await using var database = await fixture.CreateFreshDatabaseAsync(TestContext.Current.CancellationToken);
        await AddAsync(database, "Only one", May);

        var page = await ListAsync(database, Query() with { Page = 3 });

        Assert.Empty(page.Items);
        Assert.Equal(1, page.TotalCount);
    }

    [Fact]
    public async Task The_date_range_includes_its_start_and_excludes_its_end()
    {
        await using var database = await fixture.CreateFreshDatabaseAsync(TestContext.Current.CancellationToken);
        await AddAsync(database, "Before", May.AddTicks(-1));
        var atStart = await AddAsync(database, "At start", May);
        await AddAsync(database, "At end", May.AddDays(1));

        var page = await ListAsync(database, Query() with { StartsFrom = May, StartsBefore = May.AddDays(1) });

        Assert.Equal([atStart], page.Items.Select(item => item.Id));
        Assert.Equal(1, page.TotalCount);
    }

    [Fact]
    public async Task Filters_by_status()
    {
        // Only drafts can exist until Publish and Cancel arrive (1.4f), so a Published filter must find nothing.
        await using var database = await fixture.CreateFreshDatabaseAsync(TestContext.Current.CancellationToken);
        var draft = await AddAsync(database, "Draft", May);

        var drafts = await ListAsync(database, Query() with { Status = EventStatus.Draft });
        var published = await ListAsync(database, Query() with { Status = EventStatus.Published });

        Assert.Equal([draft], drafts.Items.Select(item => item.Id));
        Assert.Empty(published.Items);
    }

    private static ListEventsQuery Query() => new(1, ListEventsQuery.DefaultPageSize, null, null, null);

    private static async Task<Guid> AddAsync(EventsDatabaseFixture.FreshDatabase database, string title, DateTimeOffset startsAt)
    {
        var @event = Event.Create(title, null, startsAt, startsAt.AddHours(3), DateTimeOffset.UtcNow).Value;

        await using var scope = database.Services.CreateAsyncScope();
        scope.ServiceProvider.GetRequiredService<IEventRepository>().Add(@event);
        await scope.ServiceProvider.GetRequiredService<IUnitOfWork>().SaveChangesAsync(TestContext.Current.CancellationToken);

        return @event.Id;
    }

    private static async Task<PagedResponse<EventResponse>> ListAsync(
        EventsDatabaseFixture.FreshDatabase database,
        ListEventsQuery query)
    {
        await using var scope = database.Services.CreateAsyncScope();

        return await scope.ServiceProvider.GetRequiredService<IEventQueries>().ListAsync(query, TestContext.Current.CancellationToken);
    }
}
