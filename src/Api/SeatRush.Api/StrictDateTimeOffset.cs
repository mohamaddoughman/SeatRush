using System.Globalization;

namespace SeatRush.Api;

/// <summary>
/// The one definition of an accepted date-time for the whole API: ISO 8601 with a <c>Z</c> or an explicit offset
/// (<c>2030-05-01T20:00:00Z</c>, <c>2030-05-01T22:00:00.5+02:00</c>). Used for JSON bodies and query strings alike.
/// </summary>
/// <remarks>
/// Strict on purpose: without an offset the instant would depend on the server's time zone, and culture formats
/// like <c>05/01/2030</c> are ambiguous (May 1 or January 5).
/// </remarks>
internal static class StrictDateTimeOffset
{
    public const string ErrorMessage =
        "Must be an ISO 8601 date-time with a UTC offset, e.g. 2030-05-01T20:00:00Z or 2030-05-01T22:00:00+02:00.";

    // Seconds and fractions are optional; the offset isn't. 'Z' is matched literally, hence AssumeUniversal below,
    // which never applies to the other formats because they carry their own offset.
    private static readonly string[] Formats =
    [
        "yyyy-MM-dd'T'HH:mm:ss.FFFFFFF'Z'",
        "yyyy-MM-dd'T'HH:mm:ss'Z'",
        "yyyy-MM-dd'T'HH:mm'Z'",
        "yyyy-MM-dd'T'HH:mm:ss.FFFFFFFzzz",
        "yyyy-MM-dd'T'HH:mm:sszzz",
        "yyyy-MM-dd'T'HH:mmzzz",
    ];

    public static bool TryParse(string? text, out DateTimeOffset value) =>
        DateTimeOffset.TryParseExact(
            text,
            Formats,
            CultureInfo.InvariantCulture,
            DateTimeStyles.AssumeUniversal,
            out value);
}
