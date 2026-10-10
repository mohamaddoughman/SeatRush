namespace SeatRush.Events.Application.Abstractions;

/// <summary>
/// Saves every change a handler made through the Events repositories, in one transaction.
/// </summary>
/// <remarks>
/// Why per module instead of in Shared: each module has its own DbContext, so each has its own unit of work.
/// A single shared interface would make five modules register the same service, and one transaction can't
/// span modules anyway. Cross-module changes go through integration events (ADR 0001).
/// </remarks>
public interface IUnitOfWork
{
    Task SaveChangesAsync(CancellationToken cancellationToken);
}
