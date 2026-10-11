using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace SeatRush.Events.IntegrationTests;

/// <summary>
/// The Events endpoints over real HTTP: routing, model binding, validation, problem details and the database,
/// all through the Api's actual pipeline.
/// </summary>
public sealed class EventsApiTests(EventsDatabaseFixture database) : IDisposable
{
    private static readonly DateTimeOffset StartsAt = new(2030, 5, 1, 20, 0, 0, TimeSpan.Zero);

    private readonly HttpClient _client = database.CreateApiClient();

    [Fact]
    public async Task Post_creates_the_event_and_returns_201_with_its_location()
    {
        var response = await PostAsync(new { title = "Rock Night", description = "Live music", startsAt = StartsAt, endsAt = StartsAt.AddHours(3) });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var id = (await ReadJsonAsync(response)).GetProperty("id").GetGuid();
        Assert.Equal($"/events/{id}", response.Headers.Location?.AbsolutePath);
    }

    [Fact]
    public async Task A_created_event_can_be_read_from_its_location()
    {
        var created = await PostAsync(new { title = "Rock Night", description = "Live music", startsAt = StartsAt, endsAt = StartsAt.AddHours(3) });

        var response = await _client.GetAsync(created.Headers.Location, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await ReadJsonAsync(response);
        Assert.Equal("Rock Night", body.GetProperty("title").GetString());
        Assert.Equal("Live music", body.GetProperty("description").GetString());
        Assert.Equal(StartsAt, body.GetProperty("startsAt").GetDateTimeOffset());
        Assert.Equal("Draft", body.GetProperty("status").GetString());
    }

    [Fact]
    public async Task Post_returns_400_with_every_field_error_at_once()
    {
        // No title at all, and an end that isn't after the start.
        var response = await PostAsync(new { startsAt = StartsAt, endsAt = StartsAt });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        var errors = (await ReadJsonAsync(response)).GetProperty("errors");
        Assert.True(errors.TryGetProperty("title", out _));
        Assert.True(errors.TryGetProperty("endsAt", out _));
    }

    [Fact]
    public async Task Post_returns_400_with_the_domain_error_code_when_a_domain_rule_fails()
    {
        // Passes the validator (shape is fine) but the domain rejects it: the clock says it's in the past.
        var startsInThePast = DateTimeOffset.UtcNow.AddDays(-1);

        var response = await PostAsync(new { title = "Rock Night", startsAt = startsInThePast, endsAt = startsInThePast.AddHours(3) });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("Events.StartsInPast", (await ReadJsonAsync(response)).GetProperty("code").GetString());
    }

    [Fact]
    public async Task Post_returns_400_when_a_date_time_has_no_utc_offset()
    {
        // Without Z or an offset the instant depends on the server's time zone, so it's rejected, not guessed.
        var response = await PostAsync(new { title = "Rock Night", startsAt = "2030-05-01T20:00:00", endsAt = "2030-05-01T23:00:00Z" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var errors = (await ReadJsonAsync(response)).GetProperty("errors");
        Assert.True(errors.TryGetProperty("$.startsAt", out _));
    }

    [Fact]
    public async Task A_date_time_with_an_explicit_offset_is_kept_as_the_same_instant()
    {
        var created = await PostAsync(new { title = "Rock Night", startsAt = "2030-05-01T22:00:00+02:00", endsAt = "2030-05-02T01:00:00+02:00" });

        var response = await _client.GetAsync(created.Headers.Location, TestContext.Current.CancellationToken);

        Assert.Equal(StartsAt, (await ReadJsonAsync(response)).GetProperty("startsAt").GetDateTimeOffset());
    }

    [Fact]
    public async Task Get_returns_404_with_the_error_code_for_an_unknown_id()
    {
        var response = await _client.GetAsync($"/events/{Guid.NewGuid()}", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        Assert.Equal("Events.NotFound", (await ReadJsonAsync(response)).GetProperty("code").GetString());
    }

    [Fact]
    public async Task Get_returns_404_when_the_id_is_not_a_guid()
    {
        var response = await _client.GetAsync("/events/not-a-guid", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task The_OpenApi_document_describes_the_events_endpoints()
    {
        // WebApplicationFactory runs the Api in Development, where the document is exposed.
        var response = await _client.GetAsync("/openapi/v1.json", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var paths = (await ReadJsonAsync(response)).GetProperty("paths");
        Assert.True(paths.GetProperty("/events").TryGetProperty("post", out _));
        Assert.True(paths.GetProperty("/events/{id}").TryGetProperty("get", out _));
    }

    [Fact]
    public async Task The_OpenApi_document_still_describes_date_times_as_date_times()
    {
        // A custom JSON converter can hide a property's type from the schema generator; this one must not.
        var response = await _client.GetAsync("/openapi/v1.json", TestContext.Current.CancellationToken);

        var startsAt = (await ReadJsonAsync(response))
            .GetProperty("components").GetProperty("schemas")
            .GetProperty("CreateEventCommand").GetProperty("properties").GetProperty("startsAt");
        Assert.Equal("date-time", startsAt.GetProperty("format").GetString());
    }

    public void Dispose() => _client.Dispose();

    private Task<HttpResponseMessage> PostAsync(object body) =>
        _client.PostAsJsonAsync("/events", body, TestContext.Current.CancellationToken);

    private static async Task<JsonElement> ReadJsonAsync(HttpResponseMessage response) =>
        await response.Content.ReadFromJsonAsync<JsonElement>(TestContext.Current.CancellationToken);
}
