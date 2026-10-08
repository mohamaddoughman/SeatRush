namespace SeatRush.Shared.Messaging;

/// <summary>
/// A request to read state without changing it (CQRS read side).
/// </summary>
public interface IQuery<TResult>
{
}
