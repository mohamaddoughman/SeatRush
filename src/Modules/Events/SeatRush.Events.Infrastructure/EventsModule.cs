using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using SeatRush.Events.Application.Abstractions;
using SeatRush.Events.Application.Features.CreateEvent;
using SeatRush.Events.Application.Features.GetEventById;
using SeatRush.Events.Application.Features.ListEvents;
using SeatRush.Events.Infrastructure.Persistence;
using SeatRush.Shared.Messaging;
using SeatRush.Shared.Results;

namespace SeatRush.Events.Infrastructure;

public static class EventsModule
{
    /// <summary>
    /// Name of the connection string, matching the database resource in the Aspire AppHost.
    /// </summary>
    public const string ConnectionStringName = "seatrush";

    /// <summary>
    /// Registers the Events module's services (persistence, handlers, validators). Called once by each host (the
    /// composition roots): the Api and the migration service. HTTP endpoints are added separately
    /// (<see cref="AddEventsEndpoints"/>).
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

        // One line per use case, no assembly scanning: what the module offers is visible here.
        services.AddScoped<ICommandHandler<CreateEventCommand, Result<Guid>>, CreateEventHandler>();
        services.AddScoped<IQueryHandler<GetEventByIdQuery, Result<EventResponse>>, GetEventByIdHandler>();
        services.AddScoped<IQueryHandler<ListEventsQuery, Result<PagedResponse<EventResponse>>>, ListEventsHandler>();

        // Validators are stateless, so one instance is shared.
        services.AddSingleton<IValidator<CreateEventCommand>, CreateEventValidator>();
        services.AddSingleton<IValidator<ListEventsQuery>, ListEventsValidator>();

        // TryAdd: the host (or a test) may register its own clock first.
        services.TryAddSingleton(TimeProvider.System);

        return services;
    }

    /// <summary>
    /// Adds the Events module's controllers to a web host. Called only by the Api, next to <see cref="AddEventsModule"/>.
    /// </summary>
    /// <remarks>
    /// Separate from <see cref="AddEventsModule"/> so a host that doesn't serve HTTP (the migration service, later
    /// Azure Functions) gets the module's services without the MVC stack. The controllers live in this assembly
    /// (ADR 0002); the host discovers them through this application part.
    /// </remarks>
    public static IMvcBuilder AddEventsEndpoints(this IMvcBuilder mvc)
    {
        ArgumentNullException.ThrowIfNull(mvc);

        return mvc.AddApplicationPart(typeof(EventsModule).Assembly);
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

    /// <summary>
    /// Adds the sample events that aren't in the database yet and returns how many were added. For local
    /// development only: called by the migration service after migrating, in Development, never by the Api.
    /// </summary>
    public static async Task<int> SeedEventsDatabaseAsync(this IServiceProvider services, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(services);

        await using var scope = services.CreateAsyncScope();

        // Created here rather than registered, so no host (the Api included) gets the seeder in its container.
        var seeder = new EventsSeeder(
            scope.ServiceProvider.GetRequiredService<EventsDbContext>(),
            scope.ServiceProvider.GetRequiredService<TimeProvider>());

        return await seeder.SeedAsync(cancellationToken);
    }
}
