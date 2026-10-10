using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SeatRush.Events.Infrastructure;
using SeatRush.Events.IntegrationTests;
using Testcontainers.MsSql;

[assembly: AssemblyFixture(typeof(EventsDatabaseFixture))]

namespace SeatRush.Events.IntegrationTests;

/// <summary>
/// One SQL Server container for the whole test project, migrated once with the real migrations.
/// </summary>
/// <remarks>
/// One container, not one per class: SQL Server takes 10–20 seconds to start. Tests stay independent by
/// creating their own events (new GUIDs) and checking only those, so nothing is cleaned up and they can run
/// in parallel. This breaks down once a test lists or counts all rows; then reset the data between tests.
/// </remarks>
public sealed class EventsDatabaseFixture : IAsyncLifetime
{
    // Same image as the Aspire AppHost pins (AppHost.cs), so tests and local runs share a SQL Server version.
    private const string SqlServerImage = "mcr.microsoft.com/mssql/server:2022-latest";

    private readonly MsSqlContainer _container = new MsSqlBuilder(SqlServerImage).Build();

    private ServiceProvider? _services;

    /// <summary>The Events module wired up the way a host wires it, against the test database.</summary>
    public IServiceProvider Services =>
        _services ?? throw new InvalidOperationException("The fixture has not been initialized.");

    public string ConnectionString { get; private set; } = string.Empty;

    public async ValueTask InitializeAsync()
    {
        var cancellationToken = TestContext.Current.CancellationToken;

        await _container.StartAsync(cancellationToken);

        ConnectionString = new SqlConnectionStringBuilder(_container.GetConnectionString())
        {
            InitialCatalog = EventsModule.ConnectionStringName,
        }.ConnectionString;

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                [$"ConnectionStrings:{EventsModule.ConnectionStringName}"] = ConnectionString,
            })
            .Build();

        _services = new ServiceCollection()
            .AddEventsModule(configuration)
            .BuildServiceProvider(validateScopes: true);

        // The same call the migration service makes: proves the migrations work on an empty database.
        await _services.MigrateEventsDatabaseAsync(cancellationToken);
    }

    public async ValueTask DisposeAsync()
    {
        if (_services is not null)
        {
            await _services.DisposeAsync();
        }

        await _container.DisposeAsync();
    }
}
