using System.Text.Json;
using System.Text.Json.Serialization;

namespace SeatRush.Api.Json;

/// <summary>
/// Reads a <see cref="DateTimeOffset"/> only when the client says which instant it means: a <c>Z</c> or an
/// explicit offset (<c>2030-05-01T20:00:00Z</c>, <c>2030-05-01T22:00:00+02:00</c>).
/// </summary>
/// <remarks>
/// The default converter reads a value without an offset as the server's local time, so the same request would
/// store different instants on machines in different time zones, silently. Rejecting it turns that into a 400
/// for the field. Applies to nullable values too (System.Text.Json reuses this converter for them). What counts
/// as valid is defined once, in <see cref="StrictDateTimeOffset"/>, shared with the query-string binder.
/// </remarks>
internal sealed class StrictDateTimeOffsetConverter : JsonConverter<DateTimeOffset>
{
    public override DateTimeOffset Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) =>
        reader.TokenType == JsonTokenType.String && StrictDateTimeOffset.TryParse(reader.GetString(), out var value)
            ? value
            : throw new JsonException(StrictDateTimeOffset.ErrorMessage);

    public override void Write(Utf8JsonWriter writer, DateTimeOffset value, JsonSerializerOptions options) =>
        writer.WriteStringValue(value);
}
