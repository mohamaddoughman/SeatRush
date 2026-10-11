using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SeatRush.Events.Infrastructure;
using SeatRush.Events.IntegrationTests;
using Testcontainers.MsSql;

[assembly: AssemblyFixture(typeof(EventsDatabaseFixture))]

namespace SeatRush.Events.IntegrationTests;

/// <summary>
/// One SQL Server container for the whole test project, migrated once with the real migrations, plus the real
/// Api running in memory against it.
/// </summary>
/// <remarks>
/// One container, not one per class: SQL Server takes 10–20 seconds to start. Tests stay independent by
/// creating their own events (new GUIDs) and checking only those, so nothing is cleaned up and they can run
/// in parallel. A test that must know every row (counts, seeding) uses <see cref="CreateFreshDatabaseAsync"/>.
/// </remarks>
public sealed class EventsDatabaseFixture : IAsyncLifetime
{
    // Same image as the Aspire AppHost pins (AppHost.cs), so tests and local runs share a SQL Server version.
    private const string SqlServerImage = "mcr.microsoft.com/mssql/server:2022-latest";

    private readonly MsSqlContainer _container = new MsSqlBuilder(SqlServerImage).Build();

    private ServiceProvider? _services;

    private ApiFactory? _api;

    /// <summary>The Events module wired up the way a host wires it, against the test database.</summary>
    public IServiceProvider Services =>
        _services ?? throw new InvalidOperationException("The fixture has not been initialized.");

    public string ConnectionString { get; private set; } = string.Empty;

    /// <summary>An HTTP client for the real Api (same pipeline, middleware and configuration), in memory.</summary>
    public HttpClient CreateApiClient() =>
        (_api ?? throw new InvalidOperationException("The fixture has not been initialized.")).CreateClient();

    public async ValueTask InitializeAsync()
    {
        var cancellationToken = TestContext.Current.CancellationToken;

        await _container.StartAsync(cancellationToken);

        ConnectionString = new SqlConnectionStringBuilder(_container.GetConnectionString())
        {
            InitialCatalog = EventsModule.ConnectionStringName,
        }.ConnectionString;

        _services = CreateModuleServices(ConnectionString);

        // The same call the migration service makes: proves the migrations work on an empty database.
        await _services.MigrateEventsDatabaseAsync(cancellationToken);

        // The Api reads the same connection string name as in Aspire; here it points at the test container.
        _api = new ApiFactory(ConnectionString);

        // Start the server now rather than on the first CreateClient(): that call isn't thread-safe and runs from
        // test-class constructors in parallel, and a startup failure should fail here, not inside some test.
        _ = _api.Server;
    }

    public async ValueTask DisposeAsync()
    {
        if (_api is not null)
        {
            await _api.DisposeAsync();
        }

        if (_services is not null)
        {
            await _services.DisposeAsync();
        }

        await _container.DisposeAsync();
    }

    /// <summary>
    /// A new, migrated database of its own in the same container, for a test that must know every row (the shared
    /// database has rows from other tests). The caller disposes it.
    /// </summary>
    /// <remarks>Takes a second or two per database, so use it only when a test can't work with its own rows.</remarks>
    public async Task<FreshDatabase> CreateFreshDatabaseAsync(CancellationToken cancellationToken)
    {
        var connectionString = new SqlConnectionStringBuilder(ConnectionString)
        {
            InitialCatalog = $"{EventsModule.ConnectionStringName}_{Guid.NewGuid():N}",
        }.ConnectionString;

        var services = CreateModuleServices(connectionString);

        // Migrating creates the database first.
        await services.MigrateEventsDatabaseAsync(cancellationToken);

        return new FreshDatabase(services, connectionString);
    }

    private static ServiceProvider CreateModuleServices(string connectionString)
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                [$"ConnectionStrings:{EventsModule.ConnectionStringName}"] = connectionString,
            })
            .Build();

        return new ServiceCollection()
            .AddEventsModule(configuration)
            .BuildServiceProvider(validateScopes: true);
    }

    /// <summary>A database of one test's own: the module wired up against it, and its connection string.</summary>
    public sealed class FreshDatabase(ServiceProvider services, string connectionString) : IAsyncDisposable
    {
        public IServiceProvider Services => services;

        public string ConnectionString => connectionString;

        // The database itself goes with the container at the end of the run.
        public ValueTask DisposeAsync() => services.DisposeAsync();
    }

    /// <summary>The real Api (its Program), with the connection string pointed at the test container.</summary>
    private sealed class ApiFactory(string connectionString) : WebApplicationFactory<Program>
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder) =>
            builder.UseSetting($"ConnectionStrings:{EventsModule.ConnectionStringName}", connectionString);
    }
}
