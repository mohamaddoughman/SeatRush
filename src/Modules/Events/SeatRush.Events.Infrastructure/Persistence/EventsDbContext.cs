using Microsoft.EntityFrameworkCore;
using SeatRush.Events.Domain.Events;

namespace SeatRush.Events.Infrastructure.Persistence;

/// <summary>
/// The Events module's own DbContext. Every table it maps lives in the <see cref="Schema"/> schema.
/// </summary>
/// <remarks>
/// Internal: only this module's repositories, queries and unit of work use it. Other modules can't reach
/// the Events tables, so the module boundary also holds in the database code.
/// </remarks>
internal sealed class EventsDbContext(DbContextOptions<EventsDbContext> options) : DbContext(options)
{
    public const string Schema = "events";

    public DbSet<Event> Events => Set<Event>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(Schema);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(EventsDbContext).Assembly);
    }
}
