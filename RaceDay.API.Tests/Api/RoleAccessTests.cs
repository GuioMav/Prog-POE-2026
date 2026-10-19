using System.Net;
using System.Net.Http.Json;
using RaceDay.API.Tests.Infrastructure;

namespace RaceDay.API.Tests.Api;

/// <summary>
/// Role-based access: anonymous callers get 401 on protected endpoints, and each
/// role gets 403 on the other role's functionality.
/// </summary>
public class RoleAccessTests : ApiTestBase
{
    // {event}, {category}, ... are replaced with real seeded ids in SendAsync
    public static TheoryData<string, string> OrganiserOnlyEndpoints => new()
    {
        { "POST", "/api/events" },
        { "PUT", "/api/events/{event}" },
        { "DELETE", "/api/events/{event}" },
        { "POST", "/api/events/{event}/categories" },
        { "PUT", "/api/categories/{category}" },
        { "DELETE", "/api/categories/{category}" },
        { "GET", "/api/events/{event}/enrollments" },
        { "POST", "/api/results" },
        { "GET", "/api/organisers/{organiser}/events" }
    };

    public static TheoryData<string, string> ParticipantOnlyEndpoints => new()
    {
        { "POST", "/api/enrollments" },
        { "GET", "/api/participants/{participant}/enrollments" },
        { "GET", "/api/participants/{participant}/results" },
        { "DELETE", "/api/enrollments/1" }
    };

    public static TheoryData<string, string> AnyLoggedInEndpoints => new()
    {
        { "GET", "/api/auth/me" },
        { "GET", "/api/users/{user}" },
        { "PUT", "/api/users/{user}" },
        { "GET", "/api/enrollments/1" },
        { "PUT", "/api/enrollments/1" }
    };

    [Theory]
    [MemberData(nameof(OrganiserOnlyEndpoints))]
    public async Task Participant_IsRejectedFromOrganiserEndpoints_With403(string method, string url)
    {
        var client = await AsParticipant();

        var response = await SendAsync(client, method, url);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Theory]
    [MemberData(nameof(ParticipantOnlyEndpoints))]
    public async Task Organiser_IsRejectedFromParticipantEndpoints_With403(string method, string url)
    {
        var client = await AsOrganiser();

        var response = await SendAsync(client, method, url);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Theory]
    [MemberData(nameof(OrganiserOnlyEndpoints))]
    [MemberData(nameof(ParticipantOnlyEndpoints))]
    [MemberData(nameof(AnyLoggedInEndpoints))]
    public async Task Anonymous_IsRejectedFromProtectedEndpoints_With401(string method, string url)
    {
        var response = await SendAsync(Anonymous(), method, url);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Theory]
    [InlineData("/api/events")]
    [InlineData("/api/events/upcoming")]
    [InlineData("/api/events/{event}")]
    [InlineData("/api/events/{event}/categories")]
    [InlineData("/api/events/{event}/results")]
    public async Task Anonymous_CanUsePublicEndpoints(string url)
    {
        var response = await SendAsync(Anonymous(), "GET", url);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task RejectedRequest_DoesNotChangeData()
    {
        var participant = await AsParticipant();

        await participant.DeleteAsync($"/api/events/{Data.UpcomingEventId}");

        var stillThere = await Anonymous().GetAsync($"/api/events/{Data.UpcomingEventId}");
        Assert.Equal(HttpStatusCode.OK, stillThere.StatusCode);
    }

    private Task<HttpResponseMessage> SendAsync(HttpClient client, string method, string url)
    {
        url = url
            .Replace("{event}", Data.UpcomingEventId.ToString())
            .Replace("{category}", Data.Upcoming10kCategoryId.ToString())
            .Replace("{organiser}", Data.OrganiserId.ToString())
            .Replace("{participant}", Data.ParticipantId.ToString())
            .Replace("{user}", Data.ParticipantUserId.ToString());

        var request = new HttpRequestMessage(new HttpMethod(method), url);
        if (method is "POST" or "PUT")
            request.Content = JsonContent.Create(new { }); // the role check runs before body validation

        return client.SendAsync(request);
    }
}
