namespace SeatRush.Shared.Results;

/// <summary>
/// An expected domain or application failure, e.g. <c>Booking.SeatAlreadyHeld</c>.
/// </summary>
/// <param name="Code">Stable, machine-readable identifier: <c>&lt;Module&gt;.&lt;Reason&gt;</c>.</param>
/// <param name="Message">Human-readable description. Must not contain personal or payment data.</param>
/// <param name="Type">The kind of failure, used to choose the HTTP status code.</param>
public sealed record Error(string Code, string Message, ErrorType Type)
{
    /// <summary>The "no error" value carried by a successful <see cref="Result"/>.</summary>
    public static readonly Error None = new(string.Empty, string.Empty, ErrorType.Failure);
}
