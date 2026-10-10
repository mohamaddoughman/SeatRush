// Local development orchestration: starts SQL Server and Redis in Docker, applies the database migrations,
// then starts the Api, and wires connection strings into the projects through environment variables.
var builder = DistributedApplication.CreateBuilder(args);

// Fixed password and port, so the connection string never changes (Rider, `dotnet ef`). The password is a
// secret parameter: set it once per machine in this project's user-secrets (see README, "Running locally").
// The port isn't 1433, so it can't clash with a SQL Server installed on the machine.
const int SqlServerHostPort = 14330;
var sqlPassword = builder.AddParameter("sql-password", secret: true);

// Persistent: the container keeps running between AppHost runs and its data lives in a volume, so data
// survives restarts. To start from an empty database, delete the volume (see README).
// One database; each module gets its own schema inside it (CLAUDE.md, "Database").
// The image tag is pinned (not left to the Aspire package's default), so an Aspire upgrade can't silently
// change the SQL Server version. Keep it in sync with the integration tests (EventsDatabaseFixture).
var database = builder.AddSqlServer("sql", sqlPassword, SqlServerHostPort)
    .WithImageTag("2022-latest")
    .WithDataVolume()
    .WithLifetime(ContainerLifetime.Persistent)
    .AddDatabase("seatrush");

var redis = builder.AddRedis("redis");

// Runs once and exits. The Api waits for it to finish successfully, so it never starts against an old schema.
var migrations = builder.AddProject<Projects.SeatRush_MigrationService>("migrations")
    .WithReference(database)
    .WaitFor(database);

builder.AddProject<Projects.SeatRush_Api>("api")
    .WithReference(database)
    .WaitFor(database)
    .WaitForCompletion(migrations)
    .WithReference(redis)
    .WaitFor(redis)
    .WithHttpHealthCheck("/health");

await builder.Build().RunAsync();
