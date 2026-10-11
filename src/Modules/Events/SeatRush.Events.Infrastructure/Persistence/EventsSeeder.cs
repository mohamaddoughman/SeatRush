using Microsoft.EntityFrameworkCore;
using SeatRush.Events.Domain.Events;

namespace SeatRush.Events.Infrastructure.Persistence;

/// <summary>
/// Sample events for a local development database, so a fresh or reset database is usable in seconds.
/// Run by the migration service in Development only; never part of a migration, so no other environment gets them.
/// </summary>
/// <remarks>
/// Every sample goes through <see cref="Event.Create"/>, so sample data can never break a domain rule. Samples are
/// matched by title: any that's missing is added on each run, and existing ones are never changed (their dates stay
/// as first seeded; a database reset refreshes them).
/// </remarks>
internal sealed class EventsSeeder(EventsDbContext dbContext, TimeProvider timeProvider)
{
    // Dates are relative to the day of seeding, so the samples start in the future when they're created.
    // Covers the shapes the model allows: an evening show, a multi-day event, no description, soon and far away.
    private static readonly SampleEvent[] Samples =
    [
        new(
            "Stand-up Comedy Night",
            Description: null,
            StartsInDays: 1,
            StartTime: new TimeOnly(21, 0),
            Duration: TimeSpan.FromHours(2)),
        new(
            "City Symphony: Winter Gala",
            "An evening of Tchaikovsky and Rachmaninoff with the City Symphony Orchestra and guest pianist.",
            StartsInDays: 14,
            StartTime: new TimeOnly(19, 0),
            Duration: TimeSpan.FromHours(2.5)),
        new(
            "Hamlet at the Old Theatre",
            "A modern staging of Shakespeare's tragedy. Running time about 2 hours 45 minutes, with one interval.",
            StartsInDays: 30,
            StartTime: new TimeOnly(19, 30),
            Duration: TimeSpan.FromMinutes(165)),
        new(
            "Riverside Music Festival",
            "Three days of live music on four stages by the river, with food trucks and camping.",
            StartsInDays: 90,
            StartTime: new TimeOnly(12, 0),
            Duration: TimeSpan.FromDays(2) + TimeSpan.FromHours(11)),
        new(
            "Cloud Native Days",
            "A two-day conference on cloud architecture, distributed systems and platform engineering.",
            StartsInDays: 180,
            StartTime: new TimeOnly(9, 0),
            Duration: TimeSpan.FromDays(1) + TimeSpan.FromHours(8)),
    ];

    /// <summary>Adds every sample event that isn't in the database yet. Returns how many were added.</summary>
    public async Task<int> SeedAsync(CancellationToken cancellationToken)
    {
        var sampleTitles = Samples.Select(sample => sample.Title).ToArray();
        var existingTitles = await dbContext.Events
            .AsNoTracking()
            .Where(@event => sampleTitles.Contains(@event.Title))
            .Select(@event => @event.Title)
            .ToListAsync(cancellationToken);

        var now = timeProvider.GetUtcNow();

        // Case-insensitive, like SQL Server's default collation that matched the titles above.
        var missing = Samples
            .Where(sample => !existingTitles.Contains(sample.Title, StringComparer.OrdinalIgnoreCase))
            .ToList();

        if (missing.Count == 0)
        {
            return 0;
        }

        foreach (var sample in missing)
        {
            dbContext.Events.Add(Create(sample, now));
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        return missing.Count;
    }

    private static Event Create(SampleEvent sample, DateTimeOffset now)
    {
        var startsAt = new DateTimeOffset(
            DateOnly.FromDateTime(now.UtcDateTime).AddDays(sample.StartsInDays),
            sample.StartTime,
            TimeSpan.Zero);

        var result = Event.Create(sample.Title, sample.Description, startsAt, startsAt + sample.Duration, now);

        // A sample the domain rejects is a bug in this file, not an expected outcome: fail loudly.
        return result.IsSuccess
            ? result.Value
            : throw new InvalidOperationException($"Sample event '{sample.Title}' is invalid: {result.Error.Code}.");
    }

    private sealed record SampleEvent(
        string Title,
        string? Description,
        int StartsInDays,
        TimeOnly StartTime,
        TimeSpan Duration);
}
