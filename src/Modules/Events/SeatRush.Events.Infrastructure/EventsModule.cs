using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SeatRush.Events.Application.Abstractions;
using SeatRush.Events.Infrastructure.Persistence;

namespace SeatRush.Events.Infrastructure;

public static class EventsModule
{
    /// <summary>
    /// Name of the connection string, matching the database resource in the Aspire AppHost.
    /// </summary>
    public const string ConnectionStringName = "seatrush";

    /// <summary>
    /// Registers everything the Events module needs. Called once by each host (the composition roots):
    /// the Api and the migration service.
    /// </summary>
    /// <remarks>
    /// Registration lives in Infrastructure because it is the only layer that can see the whole module
    /// (Domain, Application, and its own persistence/adapters). The hosts call this single entry point
    /// instead of knowing the module's internals.
    /// </remarks>
    public static IServiceCollection AddEventsModule(this IServiceCollection services, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        // The connection string is read when the DbContext is first created, not here, so a host can start
        // without a database (e.g. the Api run on its own for a health check) and fails only when it needs one.
        services.AddDbContext<EventsDbContext>(options => options.UseSqlServer(
            configuration.GetConnectionString(ConnectionStringName)
                ?? throw new InvalidOperationException($"Connection string '{ConnectionStringName}' is not configured."),
            sqlServer => sqlServer.MigrationsHistoryTable(HistoryRepository.DefaultTableName, EventsDbContext.Schema)));

        services.AddScoped<IEventRepository, EventRepository>();
        services.AddScoped<IEventQueries, EventQueries>();
        services.AddScoped<IUnitOfWork, UnitOfWork>();

        return services;
    }

    /// <summary>
    /// Applies the Events module's pending migrations. Called by the migration service, never by the Api.
    /// </summary>
    public static async Task MigrateEventsDatabaseAsync(this IServiceProvider services, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(services);

        await using var scope = services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<EventsDbContext>();

        await dbContext.Database.MigrateAsync(cancellationToken);
    }
}
