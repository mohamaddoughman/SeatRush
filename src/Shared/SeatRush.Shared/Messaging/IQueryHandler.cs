namespace SeatRush.Shared.Messaging;

/// <summary>
/// Handles exactly one query (one use case). Implementations should read with <c>AsNoTracking()</c>.
/// </summary>
public interface IQueryHandler<in TQuery, TResult>
    where TQuery : IQuery<TResult>
{
    Task<TResult> HandleAsync(TQuery query, CancellationToken cancellationToken);
}
