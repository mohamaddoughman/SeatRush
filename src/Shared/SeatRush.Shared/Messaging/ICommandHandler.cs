namespace SeatRush.Shared.Messaging;

/// <summary>
/// Handles exactly one command (one use case). Controllers inject the handler they need directly.
/// </summary>
/// <remarks>
/// Why no dispatcher/mediator: injecting <c>ICommandHandler&lt;HoldSeatCommand, Result&gt;</c> keeps the call
/// explicit. "Go to definition" lands on the interface and the handler is one click away, and stack traces have
/// no reflection layer. When not: if every handler needs the same cross-cutting step (logging, validation,
/// transactions), add decorators around the handlers rather than introducing MediatR.
/// </remarks>
public interface ICommandHandler<in TCommand, TResult>
    where TCommand : ICommand<TResult>
{
    Task<TResult> HandleAsync(TCommand command, CancellationToken cancellationToken);
}
