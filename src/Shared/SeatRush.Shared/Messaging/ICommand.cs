namespace SeatRush.Shared.Messaging;

/// <summary>
/// A request to change state (CQRS write side). <typeparamref name="TResult"/> is usually
/// <see cref="Results.Result"/> or <see cref="Results.Result{TValue}"/>.
/// </summary>
public interface ICommand<TResult>
{
}
