namespace SeatRush.Shared.Domain;

/// <summary>
/// Something meaningful that happened inside a module's domain (e.g. a seat was held).
/// Domain events stay inside their module; anything another module must react to is
/// published as an <see cref="Integration.IIntegrationEvent"/> instead.
/// </summary>
public interface IDomainEvent
{
}
