using Microsoft.Data.SqlClient;
using Microsoft.Extensions.DependencyInjection;
using SeatRush.Events.Application.Abstractions;
using SeatRush.Events.Domain.Events;
using SeatRush.Events.Infrastructure;

namespace SeatRush.Events.IntegrationTests;

/// <summary>
/// Checks the schema the migrations produce, so a mapping change that would alter a column shows up here
/// as well as in the generated migration.
/// </summary>
public sealed class MigrationTests(EventsDatabaseFixture database)
{
    [Fact]
    public async Task The_events_table_has_the_expected_columns()
    {
        var columns = await QueryAsync(
            """
            SELECT COLUMN_NAME, DATA_TYPE, CHARACTER_MAXIMUM_LENGTH, IS_NULLABLE
            FROM INFORMATION_SCHEMA.COLUMNS
            WHERE TABLE_SCHEMA = 'events' AND TABLE_NAME = 'Events'
            ORDER BY ORDINAL_POSITION
            """,
            reader => (
                reader.GetString(0),
                reader.GetString(1),
                reader.IsDBNull(2) ? (int?)null : reader.GetInt32(2),
                reader.GetString(3)));

        Assert.Equal(
            [
                ("Id", "uniqueidentifier", null, "NO"),
                ("Title", "nvarchar", 200, "NO"),
                ("Description", "nvarchar", 2000, "YES"),
                ("StartsAt", "datetimeoffset", null, "NO"),
                ("EndsAt", "datetimeoffset", null, "NO"),
                ("Status", "int", null, "NO"),
            ],
            columns);
    }

    [Fact]
    public async Task The_migration_history_is_kept_in_the_events_schema()
    {
        var historyTables = await QueryAsync(
            "SELECT TABLE_SCHEMA FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = '__EFMigrationsHistory'",
            reader => reader.GetString(0));

        Assert.Equal(["events"], historyTables);
    }

    [Fact]
    public async Task Migrating_an_up_to_date_database_keeps_the_history_and_the_data()
    {
        // The migration service runs on every AppHost start, mostly against an up-to-date database.
        var cancellationToken = TestContext.Current.CancellationToken;
        var eventId = await AddEventAsync(cancellationToken);
        var historyBefore = await AppliedMigrationsAsync();

        await database.Services.MigrateEventsDatabaseAsync(cancellationToken);

        Assert.Equal(historyBefore, await AppliedMigrationsAsync());
        await using var scope = database.Services.CreateAsyncScope();
        var queries = scope.ServiceProvider.GetRequiredService<IEventQueries>();
        Assert.NotNull(await queries.GetByIdAsync(eventId, cancellationToken));
    }

    private Task<List<string>> AppliedMigrationsAsync() =>
        QueryAsync("SELECT MigrationId FROM [events].[__EFMigrationsHistory] ORDER BY MigrationId", reader => reader.GetString(0));

    private async Task<Guid> AddEventAsync(CancellationToken cancellationToken)
    {
        var startsAt = new DateTimeOffset(2030, 5, 1, 20, 0, 0, TimeSpan.Zero);
        var @event = Event.Create("Rock Night", null, startsAt, startsAt.AddHours(3), DateTimeOffset.UtcNow).Value;

        await using var scope = database.Services.CreateAsyncScope();
        scope.ServiceProvider.GetRequiredService<IEventRepository>().Add(@event);
        await scope.ServiceProvider.GetRequiredService<IUnitOfWork>().SaveChangesAsync(cancellationToken);

        return @event.Id;
    }

    private async Task<List<T>> QueryAsync<T>(string sql, Func<SqlDataReader, T> read)
    {
        var cancellationToken = TestContext.Current.CancellationToken;

        await using var connection = new SqlConnection(database.ConnectionString);
        await connection.OpenAsync(cancellationToken);

        await using var command = new SqlCommand(sql, connection);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);

        var rows = new List<T>();
        while (await reader.ReadAsync(cancellationToken))
        {
            rows.Add(read(reader));
        }

        return rows;
    }
}
