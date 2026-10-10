using SeatRush.Events.Infrastructure;

// Runs once and exits: applies every module's pending migrations. The AppHost starts the Api only after
// this process exits successfully (WaitForCompletion). A failure exits with code 1, so the Api never starts
// against the wrong schema.
var builder = Host.CreateApplicationBuilder(args);

builder.AddServiceDefaults();

builder.Services.AddEventsModule(builder.Configuration);

using var host = builder.Build();

var stopping = host.Services.GetRequiredService<IHostApplicationLifetime>().ApplicationStopping;
var logger = host.Services.GetRequiredService<ILogger<Program>>();

// Started so the OpenTelemetry log exporter runs and the logs reach the Aspire dashboard.
await host.StartAsync(stopping);

try
{
    await host.Services.MigrateEventsDatabaseAsync(stopping);
    logger.LogInformation("Database migrations applied.");
}
catch (Exception exception)
{
    // Logged, not just thrown, so the failure shows up in the dashboard's structured logs.
    logger.LogCritical(exception, "Database migrations failed. The Api will not start.");
    Environment.ExitCode = 1;
}
finally
{
    // CancellationToken.None: shutting down (and flushing telemetry) must finish even when cancelled.
    await host.StopAsync(CancellationToken.None);
}
