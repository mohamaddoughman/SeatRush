using System.Text.Json;
using System.Text.Json.Serialization;

namespace SeatRush.Api.Json;

/// <summary>
/// The JSON rules of the whole API: date-times need a UTC offset, and enums are their names, never numbers.
/// </summary>
/// <remarks>
/// Applied to two sets of options that ASP.NET Core keeps apart: MVC's, which read and write controller bodies,
/// and the minimal-API ones, which the OpenAPI generator reads to describe types. Configured in one place so the
/// document can't describe a different contract than the controllers enforce.
/// </remarks>
internal static class ApiJsonConventions
{
    public static void Apply(JsonSerializerOptions options)
    {
        options.Converters.Add(new StrictDateTimeOffsetConverter());
        options.Converters.Add(new JsonStringEnumConverter(allowIntegerValues: false));
    }
}
