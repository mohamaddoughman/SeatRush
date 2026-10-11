using SeatRush.Events.Infrastructure;

// Runs once and exits: applies every module's pending migrations, then (in Development) adds sample data.
// The AppHost starts the Api only after this process exits successfully (WaitForCompletion). A migration failure
// exits with code 1, so the Api never starts against the wrong schema; a seeding failure doesn't (see below).
var builder = Host.CreateApplicationBuilder(args);

builder.AddServiceDefaults();

builder.Services.AddEventsModule(builder.Configuration);

using var host = builder.Build();

var stopping = host.Services.GetRequiredService<IHostApplicationLifetime>().ApplicationStopping;
var logger = host.Services.GetRequiredService<ILogger<Program>>();

// Started so the OpenTelemetry log exporter runs and the logs reach the Aspire dashboard.
await host.StartAsync(stopping);

var migrated = false;
try
{
    await host.Services.MigrateEventsDatabaseAsync(stopping);
    logger.LogInformation("Database migrations applied.");
    migrated = true;
}
catch (Exception exception)
{
    // Logged, not just thrown, so the failure shows up in the dashboard's structured logs.
    logger.LogCritical(exception, "Database migrations failed. The Api will not start.");
    Environment.ExitCode = 1;
}

// Sample data for local development only; no other environment ever gets it. Development comes from the launch
// profile (Properties/launchSettings.json), which Aspire and `dotnet run` use and a deployment doesn't.
if (migrated && builder.Environment.IsDevelopment())
{
    try
    {
        var added = await host.Services.SeedEventsDatabaseAsync(stopping);
        logger.LogInformation("Sample data seeded: {AddedEvents} events added.", added);
    }
    catch (Exception exception)
    {
        // An error, but not a reason to keep the Api from starting: the schema is migrated, and sample data is a
        // convenience. Blocking local development over it would cost more than the missing samples.
        logger.LogError(exception, "Seeding sample data failed. The Api will start without it.");
    }
}
else if (migrated)
{
    // Logged so a skipped seed is visible, not silent.
    logger.LogInformation("Sample data skipped: environment is {Environment}.", builder.Environment.EnvironmentName);
}

// CancellationToken.None: shutting down (and flushing telemetry) must finish even when cancelled.
await host.StopAsync(CancellationToken.None);
