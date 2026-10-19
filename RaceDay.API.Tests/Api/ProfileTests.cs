using System.Net;
using RaceDay.API.Dtos;
using RaceDay.API.Tests.Infrastructure;

namespace RaceDay.API.Tests.Api;

/// <summary>Both roles viewing and updating their own profile.</summary>
public class ProfileTests : ApiTestBase
{
    [Fact]
    public async Task User_ViewsOwnProfile_Returns200()
    {
        var response = await (await AsParticipant()).GetAsync($"/api/users/{Data.ParticipantUserId}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var profile = await ReadAsync<UserProfileResponse>(response);
        Assert.Equal(TestData.ParticipantEmail, profile.Email);
        Assert.Equal("Thandi Nkosi", profile.Name);
    }

    [Fact]
    public async Task User_ViewsAnotherUsersProfile_Returns403()
    {
        var response = await (await AsOtherParticipant()).GetAsync($"/api/users/{Data.ParticipantUserId}");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Participant_UpdatesOwnProfile_Returns200_AndSavesName()
    {
        var client = await AsParticipant();

        var response = await PutJson(client, $"/api/users/{Data.ParticipantUserId}", new
        {
            email = "thandi.nkosi@test.co.za",
            userName = "ThandiNkosi",
            name = "Thandi M. Nkosi",
            dateOfBirth = "1998-05-01"
        });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var me = await ReadAsync<UserProfileResponse>(await client.GetAsync("/api/auth/me"));
        Assert.Equal("Thandi M. Nkosi", me.Name);
        Assert.Equal("thandi.nkosi@test.co.za", me.Email);
    }

    [Fact]
    public async Task Organiser_UpdatesOwnProfile_ChangesOrganisationName()
    {
        var client = await AsOrganiser();
        var me = await ReadAsync<UserProfileResponse>(await client.GetAsync("/api/auth/me"));

        var response = await PutJson(client, $"/api/users/{me.UsersId}", new
        {
            email = TestData.OrganiserEmail,
            userName = "SowetoRunners",
            name = "Soweto Runners & Walkers Club"
        });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("Soweto Runners & Walkers Club", (await ReadAsync<UserProfileResponse>(response)).Name);
    }

    [Fact]
    public async Task User_UpdatesProfileWithEmailInUse_Returns409()
    {
        var response = await PutJson(await AsParticipant(), $"/api/users/{Data.ParticipantUserId}", new
        {
            email = TestData.OtherParticipantEmail,
            userName = "ThandiRuns",
            name = "Thandi Nkosi"
        });

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task User_UpdatesAnotherUsersProfile_Returns403()
    {
        var response = await PutJson(await AsOtherParticipant(), $"/api/users/{Data.ParticipantUserId}", new
        {
            email = "hacked@test.co.za",
            userName = "Hacked",
            name = "Hacked"
        });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }
}
