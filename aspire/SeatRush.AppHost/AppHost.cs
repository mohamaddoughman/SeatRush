// Local development orchestration: starts SQL Server and Redis in Docker, then the Api,
// and wires connection strings into the Api through environment variables.
var builder = DistributedApplication.CreateBuilder(args);

// One database; each module gets its own schema inside it (CLAUDE.md, "Database").
var database = builder.AddSqlServer("sql")
    .AddDatabase("seatrush");

var redis = builder.AddRedis("redis");

builder.AddProject<Projects.SeatRush_Api>("api")
    .WithReference(database)
    .WaitFor(database)
    .WithReference(redis)
    .WaitFor(redis)
    .WithHttpHealthCheck("/health");

await builder.Build().RunAsync();
