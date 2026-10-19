using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using RaceDay.API.Dtos;

namespace RaceDay.API.Tests.Infrastructure;

/// <summary>
/// Base class for API tests. xUnit creates a new instance per test, so every test
/// gets its own in-memory API and database seeded with <see cref="TestData"/>.
/// Each HttpClient keeps its own cookies, so one client = one logged-in session.
/// </summary>
public abstract class ApiTestBase : IAsyncLifetime
{
    protected static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    protected RaceDayApiFactory Factory { get; } = new();
    protected TestData Data { get; private set; } = null!;

    public virtual async Task InitializeAsync() => Data = await Factory.WithDbAsync(TestData.SeedAsync);

    public async Task DisposeAsync() => await Factory.DisposeAsync();

    /// <summary>A client with no session (not logged in).</summary>
    protected HttpClient Anonymous() => Factory.CreateClient();

    /// <summary>A client that has logged in as the given user and holds the session cookie.</summary>
    protected async Task<HttpClient> LoginAsync(string email, string password = TestData.Password)
    {
        var client = Factory.CreateClient();
        var response = await client.PostAsJsonAsync("/api/auth/login", new LoginRequest { Email = email, Password = password });
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return client;
    }

    protected Task<HttpClient> AsOrganiser() => LoginAsync(TestData.OrganiserEmail);
    protected Task<HttpClient> AsOtherOrganiser() => LoginAsync(TestData.OtherOrganiserEmail);
    protected Task<HttpClient> AsParticipant() => LoginAsync(TestData.ParticipantEmail);
    protected Task<HttpClient> AsOtherParticipant() => LoginAsync(TestData.OtherParticipantEmail);

    protected static async Task<T> ReadAsync<T>(HttpResponseMessage response) =>
        (await response.Content.ReadFromJsonAsync<T>(Json))!;

    protected static Task<HttpResponseMessage> PostJson(HttpClient client, string url, object body) =>
        client.PostAsJsonAsync(url, body, Json);

    protected static Task<HttpResponseMessage> PutJson(HttpClient client, string url, object body) =>
        client.PutAsJsonAsync(url, body, Json);

    /// <summary>A valid event request that tests can tweak.</summary>
    protected static EventRequest NewEventRequest(int daysFromToday = 14) => new()
    {
        EventName = "Durban Beachfront Ride",
        EventDate = TestData.Today.AddDays(daysFromToday),
        Description = "A flat coastal ride along the promenade.",
        Location = "uShaka Beach, Durban",
        DistanceKm = 40,
        EventType = Models.EventType.Cycle,
        EntryFee = 120
    };
}
