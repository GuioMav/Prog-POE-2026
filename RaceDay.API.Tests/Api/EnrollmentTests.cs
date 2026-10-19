using System.Net;
using Microsoft.EntityFrameworkCore;
using RaceDay.API.Dtos;
using RaceDay.API.Models;
using RaceDay.API.Tests.Infrastructure;

namespace RaceDay.API.Tests.Api;

/// <summary>Participants entering events by category, and organisers viewing entries.</summary>
public class EnrollmentTests : ApiTestBase
{
    private object EnterUpcoming10k() => new { eventId = Data.UpcomingEventId, categoryId = Data.Upcoming10kCategoryId };

    private async Task<EnrollmentResponse> EnrolParticipantAsync()
    {
        var response = await PostJson(await AsParticipant(), "/api/enrollments", EnterUpcoming10k());
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return await ReadAsync<EnrollmentResponse>(response);
    }

    [Fact]
    public async Task Participant_Enrols_Returns201_WithEventAndCategory()
    {
        var response = await PostJson(await AsParticipant(), "/api/enrollments", EnterUpcoming10k());

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var enrollment = await ReadAsync<EnrollmentResponse>(response);
        Assert.Equal(Data.UpcomingEventId, enrollment.EventId);
        Assert.Equal("Soweto Sunrise 10K", enrollment.EventName);
        Assert.Equal("10km", enrollment.CategoryName);
        Assert.Equal("Thandi Nkosi", enrollment.ParticipantName);
        Assert.Equal(EnrollmentStatus.Confirmed, enrollment.Status);
    }

    [Fact]
    public async Task Participant_Enrols_RecordsParticipantEventAndCategoryInDatabase()
    {
        var created = await EnrolParticipantAsync();

        var saved = await Factory.WithDbAsync(db => db.Enrollments.SingleAsync(e => e.EnrollmentId == created.EnrollmentId));
        Assert.Equal(Data.ParticipantId, saved.ParticipantId);
        Assert.Equal(Data.UpcomingEventId, saved.EventId);
        Assert.Equal(Data.Upcoming10kCategoryId, saved.CategoryId);
        Assert.Equal(80m, saved.Price); // copied from the event's entry fee
        Assert.Equal(TestData.Today, saved.EnrollmentDate);
    }

    [Fact]
    public async Task Participant_EnrolsTwiceInSameEvent_Returns409()
    {
        var client = await AsParticipant();
        await PostJson(client, "/api/enrollments", EnterUpcoming10k());

        var second = await PostJson(client, "/api/enrollments",
            new { eventId = Data.UpcomingEventId, categoryId = Data.UpcomingUnder20CategoryId });

        Assert.Equal(HttpStatusCode.Conflict, second.StatusCode);
    }

    [Fact]
    public async Task Participant_EnrolsWithCategoryFromAnotherEvent_Returns400()
    {
        var response = await PostJson(await AsParticipant(), "/api/enrollments",
            new { eventId = Data.UpcomingEventId, categoryId = Data.Past21kCategoryId });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Participant_EnrolsInPastEvent_Returns400()
    {
        var response = await PostJson(await AsParticipant(), "/api/enrollments",
            new { eventId = Data.PastEventId, categoryId = Data.Past21kCategoryId });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Theory]
    [InlineData(9999, 1)]
    [InlineData(1, 9999)]
    public async Task Participant_EnrolsWithUnknownEventOrCategory_Returns404(int eventId, int categoryId)
    {
        var response = await PostJson(await AsParticipant(), "/api/enrollments", new { eventId, categoryId });

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Participant_EnrolsWithoutCategory_Returns400()
    {
        var response = await PostJson(await AsParticipant(), "/api/enrollments", new { eventId = Data.UpcomingEventId });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Participant_SeesOwnEnrollments_ButNotSomeoneElses()
    {
        await EnrolParticipantAsync();
        var client = await AsParticipant();

        var own = await client.GetAsync($"/api/participants/{Data.ParticipantId}/enrollments");
        var other = await client.GetAsync($"/api/participants/{Data.OtherParticipantId}/enrollments");

        Assert.Single(await ReadAsync<List<EnrollmentResponse>>(own));
        Assert.Equal(HttpStatusCode.Forbidden, other.StatusCode);
    }

    [Fact]
    public async Task Organiser_ViewsAllEnrollmentsForOwnEvent()
    {
        await EnrolParticipantAsync();
        await PostJson(await AsOtherParticipant(), "/api/enrollments",
            new { eventId = Data.UpcomingEventId, categoryId = Data.UpcomingUnder20CategoryId });

        var response = await (await AsOrganiser()).GetAsync($"/api/events/{Data.UpcomingEventId}/enrollments");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var entries = await ReadAsync<List<EnrollmentResponse>>(response);
        Assert.Equal(2, entries.Count);
        Assert.Contains(entries, e => e.ParticipantName == "Pieter van Wyk" && e.CategoryName == "Under 20");
    }

    [Fact]
    public async Task Organiser_ViewsEnrollmentsForAnotherOrganisersEvent_Returns403()
    {
        var response = await (await AsOtherOrganiser()).GetAsync($"/api/events/{Data.UpcomingEventId}/enrollments");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task SingleEnrollment_VisibleToEntrantAndEventOrganiser_NotToOthers()
    {
        var enrollment = await EnrolParticipantAsync();
        var url = $"/api/enrollments/{enrollment.EnrollmentId}";

        Assert.Equal(HttpStatusCode.OK, (await (await AsParticipant()).GetAsync(url)).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await (await AsOrganiser()).GetAsync(url)).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await (await AsOtherParticipant()).GetAsync(url)).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await (await AsOtherOrganiser()).GetAsync(url)).StatusCode);
    }

    [Fact]
    public async Task Participant_CancelsOwnEnrollment_Returns200()
    {
        var enrollment = await EnrolParticipantAsync();

        var response = await PutJson(await AsParticipant(), $"/api/enrollments/{enrollment.EnrollmentId}", new { status = "Cancelled" });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(EnrollmentStatus.Cancelled, (await ReadAsync<EnrollmentResponse>(response)).Status);
    }

    [Fact]
    public async Task Participant_CannotSetStatusOtherThanCancelled()
    {
        var enrollment = await EnrolParticipantAsync();

        var response = await PutJson(await AsParticipant(), $"/api/enrollments/{enrollment.EnrollmentId}", new { status = "Pending" });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Organiser_ChangesStatusOfEntryForOwnEvent()
    {
        var enrollment = await EnrolParticipantAsync();

        var response = await PutJson(await AsOrganiser(), $"/api/enrollments/{enrollment.EnrollmentId}", new { status = "Pending" });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(EnrollmentStatus.Pending, (await ReadAsync<EnrollmentResponse>(response)).Status);
    }

    [Fact]
    public async Task CancelledEntry_CanBeReEntered()
    {
        var enrollment = await EnrolParticipantAsync();
        var client = await AsParticipant();
        await PutJson(client, $"/api/enrollments/{enrollment.EnrollmentId}", new { status = "Cancelled" });

        var again = await PostJson(client, "/api/enrollments",
            new { eventId = Data.UpcomingEventId, categoryId = Data.UpcomingUnder20CategoryId });

        Assert.Equal(HttpStatusCode.Created, again.StatusCode);
        var reEntered = await ReadAsync<EnrollmentResponse>(again);
        Assert.Equal(enrollment.EnrollmentId, reEntered.EnrollmentId);
        Assert.Equal("Under 20", reEntered.CategoryName);
        Assert.Equal(EnrollmentStatus.Confirmed, reEntered.Status);
    }

    [Fact]
    public async Task Participant_WithdrawsOwnEnrollment_Returns204()
    {
        var enrollment = await EnrolParticipantAsync();

        var response = await (await AsParticipant()).DeleteAsync($"/api/enrollments/{enrollment.EnrollmentId}");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        Assert.False(await Factory.WithDbAsync(db => db.Enrollments.AnyAsync(e => e.EnrollmentId == enrollment.EnrollmentId)));
    }

    [Fact]
    public async Task Participant_WithdrawsSomeoneElsesEnrollment_Returns403()
    {
        var enrollment = await EnrolParticipantAsync();

        var response = await (await AsOtherParticipant()).DeleteAsync($"/api/enrollments/{enrollment.EnrollmentId}");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }
}
