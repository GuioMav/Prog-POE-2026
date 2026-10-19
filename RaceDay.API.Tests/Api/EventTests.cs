using System.Net;
using Microsoft.EntityFrameworkCore;
using RaceDay.API.Dtos;
using RaceDay.API.Models;
using RaceDay.API.Tests.Infrastructure;

namespace RaceDay.API.Tests.Api;

/// <summary>Organisers managing events, and everyone browsing them.</summary>
public class EventTests : ApiTestBase
{
    [Fact]
    public async Task Organiser_CreatesEvent_Returns201_WithAllRequiredFields()
    {
        var client = await AsOrganiser();

        var response = await PostJson(client, "/api/events", NewEventRequest());

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var created = await ReadAsync<EventDetailResponse>(response);
        Assert.Equal("Durban Beachfront Ride", created.EventName);
        Assert.Equal(EventType.Cycle, created.EventType);
        Assert.Equal(40, created.DistanceKm);
        Assert.Equal("uShaka Beach, Durban", created.Location);
        Assert.Equal("A flat coastal ride along the promenade.", created.Description);
        Assert.Equal(Data.OrganiserId, created.OrganiserId);
        Assert.Equal($"/api/events/{created.EventId}", response.Headers.Location!.AbsolutePath, ignoreCase: true);
    }

    [Fact]
    public async Task Organiser_CreatesEvent_WithPastDate_Returns400()
    {
        var client = await AsOrganiser();

        var response = await PostJson(client, "/api/events", NewEventRequest(daysFromToday: -1));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Organiser_CreatesEvent_WithMissingFields_Returns400()
    {
        var client = await AsOrganiser();

        var response = await PostJson(client, "/api/events", new { eventName = "No date or type", location = "Somewhere", distanceKm = 5 });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Organiser_UpdatesOwnEvent_Returns200_AndSavesChanges()
    {
        var client = await AsOrganiser();
        var update = NewEventRequest();
        update.EventName = "Soweto Sunrise 10K (new route)";
        update.EventType = EventType.Walk;

        var response = await PutJson(client, $"/api/events/{Data.UpcomingEventId}", update);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var saved = await Factory.WithDbAsync(db => db.Events.SingleAsync(e => e.EventId == Data.UpcomingEventId));
        Assert.Equal("Soweto Sunrise 10K (new route)", saved.EventName);
        Assert.Equal(EventType.Walk, saved.EventType);
    }

    [Fact]
    public async Task Organiser_UpdatesAnotherOrganisersEvent_Returns403()
    {
        var client = await AsOtherOrganiser();

        var response = await PutJson(client, $"/api/events/{Data.UpcomingEventId}", NewEventRequest());

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Organiser_UpdatesMissingEvent_Returns404()
    {
        var client = await AsOrganiser();

        var response = await PutJson(client, "/api/events/9999", NewEventRequest());

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Organiser_DeletesOwnEvent_Returns204_AndRemovesItsCategoriesAndEntries()
    {
        var participant = await AsParticipant();
        await PostJson(participant, "/api/enrollments", new { eventId = Data.UpcomingEventId, categoryId = Data.Upcoming10kCategoryId });
        var client = await AsOrganiser();

        var response = await client.DeleteAsync($"/api/events/{Data.UpcomingEventId}");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync($"/api/events/{Data.UpcomingEventId}")).StatusCode);
        var leftovers = await Factory.WithDbAsync(async db =>
            await db.Categories.CountAsync(c => c.EventId == Data.UpcomingEventId) +
            await db.Enrollments.CountAsync(e => e.EventId == Data.UpcomingEventId));
        Assert.Equal(0, leftovers);
    }

    [Fact]
    public async Task Organiser_DeletesAnotherOrganisersEvent_Returns403()
    {
        var client = await AsOtherOrganiser();

        var response = await client.DeleteAsync($"/api/events/{Data.UpcomingEventId}");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Anyone_CanListEvents_AndViewDetailsWithCategories()
    {
        var client = Anonymous();

        var list = await ReadAsync<List<EventResponse>>(await client.GetAsync("/api/events"));
        var detail = await ReadAsync<EventDetailResponse>(await client.GetAsync($"/api/events/{Data.UpcomingEventId}"));

        Assert.Equal(2, list.Count);
        Assert.Equal("Soweto Sunrise 10K", detail.EventName);
        Assert.Equal(2, detail.Categories.Count);
        Assert.Contains(detail.Categories, c => c.CategoryName == "Under 20" && c.CategoryType == CategoryType.Age);
    }

    [Fact]
    public async Task ParticipantRole_CanAlsoViewEvents()
    {
        var client = await AsParticipant();

        var response = await client.GetAsync($"/api/events/{Data.UpcomingEventId}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task GetEvent_UnknownId_Returns404()
    {
        var response = await Anonymous().GetAsync("/api/events/9999");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task UpcomingEvents_ExcludesPastEvents()
    {
        var events = await ReadAsync<List<EventResponse>>(await Anonymous().GetAsync("/api/events/upcoming"));

        Assert.Contains(events, e => e.EventId == Data.UpcomingEventId);
        Assert.DoesNotContain(events, e => e.EventId == Data.PastEventId);
    }

    [Fact]
    public async Task ListEvents_FiltersByEventType()
    {
        var organiser = await AsOrganiser();
        await PostJson(organiser, "/api/events", NewEventRequest()); // a Cycle event

        var cycles = await ReadAsync<List<EventResponse>>(await Anonymous().GetAsync("/api/events?type=Cycle"));

        Assert.Single(cycles);
        Assert.All(cycles, e => Assert.Equal(EventType.Cycle, e.EventType));
    }

    [Fact]
    public async Task Organiser_ListsOwnEvents_ButNotAnotherOrganisers()
    {
        var client = await AsOrganiser();

        var own = await client.GetAsync($"/api/organisers/{Data.OrganiserId}/events");
        var other = await client.GetAsync($"/api/organisers/{Data.OtherOrganiserId}/events");

        Assert.Equal(HttpStatusCode.OK, own.StatusCode);
        Assert.Equal(2, (await ReadAsync<List<EventResponse>>(own)).Count);
        Assert.Equal(HttpStatusCode.Forbidden, other.StatusCode);
    }
}
