using SeatRush.Events.Application.Abstractions;

namespace SeatRush.Events.Infrastructure.Persistence;

/// <summary>
/// Saves everything the repositories tracked in this request's <see cref="EventsDbContext"/>, in one transaction.
/// </summary>
internal sealed class UnitOfWork(EventsDbContext dbContext) : IUnitOfWork
{
    public Task SaveChangesAsync(CancellationToken cancellationToken) =>
        dbContext.SaveChangesAsync(cancellationToken);
}
