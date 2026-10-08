namespace SeatRush.Shared.Integration;

/// <summary>
/// A fact published by one module for other modules to react to (e.g. <c>PaymentSucceeded</c>).
/// Lives in the publishing module's Contracts project. Delivered in-process for now and through
/// Azure Service Bus with an outbox in a later phase (ADR 0001).
/// </summary>
public interface IIntegrationEvent
{
    Guid Id { get; }

    DateTime OccurredOnUtc { get; }
}
