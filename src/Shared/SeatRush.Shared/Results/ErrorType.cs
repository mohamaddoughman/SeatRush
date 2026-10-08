namespace SeatRush.Shared.Results;

/// <summary>
/// The kind of failure. The Api maps each kind to an HTTP status code in its problem details response.
/// </summary>
public enum ErrorType
{
    Failure = 0,
    Validation = 1,
    NotFound = 2,
    Conflict = 3,
}
