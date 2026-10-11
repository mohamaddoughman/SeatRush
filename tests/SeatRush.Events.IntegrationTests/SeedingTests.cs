using Microsoft.Data.SqlClient;
using SeatRush.Events.Infrastructure;

namespace SeatRush.Events.IntegrationTests;

/// <summary>
/// The sample data the migration service adds in Development: every run adds the samples that are missing,
/// and only those.
/// </summary>
/// <remarks>
/// Each test seeds a fresh database of its own, so it knows every row, and the shared database never gets rows
/// that no test owns.
/// </remarks>
public sealed class SeedingTests(EventsDatabaseFixture fixture)
{
    [Fact]
    public async Task A_fresh_database_gets_every_sample_as_a_future_draft()
    {
        await using var database = await fixture.CreateFreshDatabaseAsync(TestContext.Current.CancellationToken);

        var added = await SeedAsync(database);

        Assert.True(added > 0);
        Assert.Equal(added, await ScalarAsync(database, "SELECT COUNT(*) FROM [events].[Events]"));
        Assert.Equal(added, await ScalarAsync(
            database,
            "SELECT COUNT(*) FROM [events].[Events] WHERE Status = 0 AND StartsAt > SYSDATETIMEOFFSET() AND EndsAt > StartsAt"));
    }

    [Fact]
    public async Task Seeding_again_adds_nothing()
    {
        await using var database = await fixture.CreateFreshDatabaseAsync(TestContext.Current.CancellationToken);
        await SeedAsync(database);

        var added = await SeedAsync(database);

        Assert.Equal(0, added);
    }

    [Fact]
    public async Task A_deleted_sample_event_is_added_again()
    {
        await using var database = await fixture.CreateFreshDatabaseAsync(TestContext.Current.CancellationToken);
        var seeded = await SeedAsync(database);
        await ScalarAsync(database, "DELETE TOP (1) FROM [events].[Events]; SELECT @@ROWCOUNT");

        var added = await SeedAsync(database);

        Assert.Equal(1, added);
        Assert.Equal(seeded, await ScalarAsync(database, "SELECT COUNT(*) FROM [events].[Events]"));
    }

    private static Task<int> SeedAsync(EventsDatabaseFixture.FreshDatabase database) =>
        database.Services.SeedEventsDatabaseAsync(TestContext.Current.CancellationToken);

    private static async Task<int> ScalarAsync(EventsDatabaseFixture.FreshDatabase database, string sql)
    {
        var cancellationToken = TestContext.Current.CancellationToken;

        await using var connection = new SqlConnection(database.ConnectionString);
        await connection.OpenAsync(cancellationToken);
        await using var command = new SqlCommand(sql, connection);

        return Convert.ToInt32(await command.ExecuteScalarAsync(cancellationToken));
    }
}
