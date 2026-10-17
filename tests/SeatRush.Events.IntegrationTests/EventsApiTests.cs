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
    public async Task List_returns_a_page_with_the_events_in_the_requested_window()
    {
        // The database is shared with other tests, so the event gets a start time no other test uses and the
        // list is narrowed to it. Sorting and paging over known rows are covered in ListEventsQueryTests.
        var startsAt = new DateTimeOffset(2100, 1, 1, 0, 0, 0, TimeSpan.Zero).AddSeconds(Random.Shared.Next());
        var created = await PostAsync(new { title = "Rock Night", startsAt, endsAt = startsAt.AddHours(3) });
        var id = (await ReadJsonAsync(created)).GetProperty("id").GetGuid();

        var response = await _client.GetAsync(
            $"/events?startsFrom={Query(startsAt)}&startsBefore={Query(startsAt.AddSeconds(1))}",
            TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var page = await ReadJsonAsync(response);

        // "Contains", not "is the only one": another event in the same second would be rare, but not impossible.
        Assert.Contains(id, page.GetProperty("items").EnumerateArray().Select(item => item.GetProperty("id").GetGuid()));
        Assert.Equal(1, page.GetProperty("page").GetInt32());
        Assert.Equal(20, page.GetProperty("pageSize").GetInt32());
    }

    [Fact]
    public async Task List_returns_400_with_every_paging_error_at_once()
    {
        var response = await _client.GetAsync("/events?page=0&pageSize=101", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var errors = (await ReadJsonAsync(response)).GetProperty("errors");
        Assert.True(errors.TryGetProperty("page", out _));
        Assert.True(errors.TryGetProperty("pageSize", out _));
    }

    [Theory]
    [InlineData("Finished")]
    [InlineData("1")]                // numbers aren't part of the contract
    [InlineData("Draft,Cancelled")]  // MVC would OR these into Cancelled
    public async Task List_returns_400_for_a_status_that_is_not_one_name(string status)
    {
        var response = await _client.GetAsync($"/events?status={status}", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.True((await ReadJsonAsync(response)).GetProperty("errors").TryGetProperty("status", out _));
    }

    [Fact]
    public async Task List_accepts_a_status_name_in_any_case()
    {
        var response = await _client.GetAsync("/events?status=published", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Theory]
    [InlineData("2030-05-01T00:00:00")]                  // no offset: would depend on the server's time zone
    [InlineData("05%2F01%2F2030%2000%3A00%20%2B02%3A00")] // has an offset, but "05/01" is ambiguous: not ISO 8601
    public async Task List_returns_400_for_a_date_filter_that_is_not_iso_8601_with_an_offset(string startsFrom)
    {
        // Same rule as in JSON bodies, enforced for query strings by the Api's model binder.
        var response = await _client.GetAsync($"/events?startsFrom={startsFrom}", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.True((await ReadJsonAsync(response)).GetProperty("errors").TryGetProperty("startsFrom", out _));
    }

    [Fact]
    public async Task Post_returns_400_for_a_date_time_that_is_not_iso_8601()
    {
        var response = await PostAsync(new { title = "Rock Night", startsAt = "05/01/2030 20:00 +02:00", endsAt = "2030-05-01T23:00:00Z" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.True((await ReadJsonAsync(response)).GetProperty("errors").TryGetProperty("$.startsAt", out _));
    }

    [Fact]
    public async Task List_accepts_a_date_filter_with_an_encoded_offset()
    {
        // '+' must be sent as %2B: a bare '+' in a query string means a space.
        var response = await _client.GetAsync("/events?startsFrom=2030-05-01T22:00:00%2B02:00", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task The_OpenApi_document_describes_the_events_endpoints()
    {
        // WebApplicationFactory runs the Api in Development, where the document is exposed.
        var response = await _client.GetAsync("/openapi/v1.json", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var paths = (await ReadJsonAsync(response)).GetProperty("paths");
        Assert.True(paths.GetProperty("/events").TryGetProperty("post", out _));
        Assert.True(paths.GetProperty("/events").TryGetProperty("get", out _));
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

    [Fact]
    public async Task The_OpenApi_document_describes_the_date_filters_as_date_times()
    {
        // Same risk for the query-string binder as for the JSON converter above.
        var response = await _client.GetAsync("/openapi/v1.json", TestContext.Current.CancellationToken);

        var startsFrom = (await ReadJsonAsync(response))
            .GetProperty("paths").GetProperty("/events").GetProperty("get").GetProperty("parameters")
            .EnumerateArray()
            .Single(parameter => parameter.GetProperty("name").GetString() == "startsFrom");
        Assert.Equal("date-time", startsFrom.GetProperty("schema").GetProperty("format").GetString());
    }

    [Fact]
    public async Task The_OpenApi_document_describes_statuses_by_name()
    {
        var response = await _client.GetAsync("/openapi/v1.json", TestContext.Current.CancellationToken);

        var status = (await ReadJsonAsync(response))
            .GetProperty("components").GetProperty("schemas").GetProperty("EventStatus");
        // The filter is optional, so the schema also allows null; only the names matter here.
        Assert.Equal(
            ["Draft", "Published", "Cancelled"],
            status.GetProperty("enum").EnumerateArray()
                .Where(value => value.ValueKind == JsonValueKind.String)
                .Select(value => value.GetString()));
    }

    public void Dispose() => _client.Dispose();

    private Task<HttpResponseMessage> PostAsync(object body) =>
        _client.PostAsJsonAsync("/events", body, TestContext.Current.CancellationToken);

    // ISO 8601 with its offset, escaped for a query string ("+00:00" would otherwise arrive as " 00:00").
    private static string Query(DateTimeOffset value) => Uri.EscapeDataString(value.ToString("O"));

    private static async Task<JsonElement> ReadJsonAsync(HttpResponseMessage response) =>
        await response.Content.ReadFromJsonAsync<JsonElement>(TestContext.Current.CancellationToken);
}
