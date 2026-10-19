using System.Net;
using RaceDay.API.Dtos;
using RaceDay.API.Models;
using RaceDay.API.Tests.Infrastructure;

namespace RaceDay.API.Tests.Api;

/// <summary>Organisers capturing results after an event; participants viewing their history.</summary>
public class ResultTests : ApiTestBase
{
    public override async Task InitializeAsync()
    {
        await base.InitializeAsync();

        // Both participants took part in the past event
        await Factory.WithDbAsync(async db =>
        {
            db.Enrollments.AddRange(
                new Enrollment { ParticipantId = Data.ParticipantId, EventId = Data.PastEventId, CategoryId = Data.Past21kCategoryId, EnrollmentDate = TestData.Today.AddDays(-30), Price = 150 },
                new Enrollment { ParticipantId = Data.OtherParticipantId, EventId = Data.PastEventId, CategoryId = Data.Past21kCategoryId, EnrollmentDate = TestData.Today.AddDays(-30), Price = 150 });
            return await db.SaveChangesAsync();
        });
    }

    private object ResultFor(int participantId, string time, int position, int? eventId = null) => new
    {
        participantId,
        eventId = eventId ?? Data.PastEventId,
        finishingTime = time,
        position
    };

    [Fact]
    public async Task Organiser_RecordsResult_Returns201()
    {
        var response = await PostJson(await AsOrganiser(), "/api/results", ResultFor(Data.ParticipantId, "01:42:10", 1));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var result = await ReadAsync<ResultResponse>(response);
        Assert.Equal(new TimeSpan(1, 42, 10), result.FinishingTime);
        Assert.Equal(1, result.Position);
        Assert.Equal("21km", result.CategoryName);
    }

    [Fact]
    public async Task Organiser_RecordsResultBeforeEventHasHappened_Returns400()
    {
        await PostJson(await AsParticipant(), "/api/enrollments",
            new { eventId = Data.UpcomingEventId, categoryId = Data.Upcoming10kCategoryId });

        var response = await PostJson(await AsOrganiser(), "/api/results",
            ResultFor(Data.ParticipantId, "00:45:00", 1, Data.UpcomingEventId));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Organiser_RecordsResultForParticipantNotEntered_Returns400()
    {
        var newcomer = await PostJson(Anonymous(), "/api/auth/register", new
        {
            email = "sipho@test.co.za", userName = "SiphoRuns", password = "Str0ngPass!", role = "Participant", name = "Sipho Dlamini"
        });
        var siphoId = (await ReadAsync<UserProfileResponse>(newcomer)).ProfileId;

        var response = await PostJson(await AsOrganiser(), "/api/results", ResultFor(siphoId, "01:50:00", 3));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Organiser_RecordsTakenPosition_Returns409()
    {
        var organiser = await AsOrganiser();
        await PostJson(organiser, "/api/results", ResultFor(Data.ParticipantId, "01:42:10", 1));

        var response = await PostJson(organiser, "/api/results", ResultFor(Data.OtherParticipantId, "01:43:00", 1));

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task Organiser_RecordsSecondResultForSameParticipant_Returns409()
    {
        var organiser = await AsOrganiser();
        await PostJson(organiser, "/api/results", ResultFor(Data.ParticipantId, "01:42:10", 1));

        var response = await PostJson(organiser, "/api/results", ResultFor(Data.ParticipantId, "01:40:00", 2));

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task Organiser_RecordsResultForAnotherOrganisersEvent_Returns403()
    {
        var response = await PostJson(await AsOtherOrganiser(), "/api/results", ResultFor(Data.ParticipantId, "01:42:10", 1));

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Theory]
    [InlineData("00:00:00", 1)]
    [InlineData("01:00:00", 0)]
    public async Task Organiser_RecordsInvalidTimeOrPosition_Returns400(string time, int position)
    {
        var response = await PostJson(await AsOrganiser(), "/api/results", ResultFor(Data.ParticipantId, time, position));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Organiser_CorrectsResult_Returns200()
    {
        var organiser = await AsOrganiser();
        var created = await ReadAsync<ResultResponse>(await PostJson(organiser, "/api/results", ResultFor(Data.ParticipantId, "01:42:10", 2)));

        var response = await PutJson(organiser, $"/api/results/{created.ResultId}", new { finishingTime = "01:41:55", position = 1 });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var updated = await ReadAsync<ResultResponse>(response);
        Assert.Equal(1, updated.Position);
        Assert.Equal(new TimeSpan(1, 41, 55), updated.FinishingTime);
    }

    [Fact]
    public async Task Participant_ViewsOwnResults_WithPositionOutOfTotalFinishers()
    {
        var organiser = await AsOrganiser();
        await PostJson(organiser, "/api/results", ResultFor(Data.OtherParticipantId, "01:38:00", 1));
        await PostJson(organiser, "/api/results", ResultFor(Data.ParticipantId, "01:42:10", 2));

        var response = await (await AsParticipant()).GetAsync($"/api/participants/{Data.ParticipantId}/results");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = Assert.Single(await ReadAsync<List<ResultResponse>>(response));
        Assert.Equal("Two Oceans Warm-up", result.EventName);
        Assert.Equal(2, result.Position);
        Assert.Equal(2, result.TotalFinishers);
    }

    [Fact]
    public async Task Participant_ViewsAnotherParticipantsResults_Returns403()
    {
        var response = await (await AsParticipant()).GetAsync($"/api/participants/{Data.OtherParticipantId}/results");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Anyone_ViewsEventLeaderboard_OrderedByPosition()
    {
        var organiser = await AsOrganiser();
        await PostJson(organiser, "/api/results", ResultFor(Data.ParticipantId, "01:42:10", 2));
        await PostJson(organiser, "/api/results", ResultFor(Data.OtherParticipantId, "01:38:00", 1));

        var leaderboard = await ReadAsync<List<ResultResponse>>(await Anonymous().GetAsync($"/api/events/{Data.PastEventId}/results"));

        Assert.Equal([1, 2], leaderboard.Select(r => r.Position));
        Assert.Equal("Pieter van Wyk", leaderboard[0].ParticipantName);
    }
}
