using System.Net;
using Microsoft.EntityFrameworkCore;
using RaceDay.API.Dtos;
using RaceDay.API.Models;
using RaceDay.API.Tests.Infrastructure;

namespace RaceDay.API.Tests.Api;

/// <summary>Registration, login, logout and session behaviour.</summary>
public class AuthTests : ApiTestBase
{
    private static object ParticipantRegistration(string email = "lerato@test.co.za", string userName = "LeratoRuns") => new
    {
        email,
        userName,
        password = "Str0ngPass!",
        role = "Participant",
        name = "Lerato Mokoena",
        dateOfBirth = "2001-08-14"
    };

    [Fact]
    public async Task Register_Participant_Returns201_AndCreatesUserAndParticipantRows()
    {
        var response = await PostJson(Anonymous(), "/api/auth/register", ParticipantRegistration());

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var profile = await ReadAsync<UserProfileResponse>(response);
        Assert.Equal(UserRole.Participant, profile.Role);
        Assert.Equal(AccountType.Standard, profile.AccountType);
        Assert.Equal("Lerato Mokoena", profile.Name);

        var participant = await Factory.WithDbAsync(db =>
            db.Participants.Include(p => p.User).SingleAsync(p => p.ParticipantId == profile.ProfileId));
        Assert.Equal("lerato@test.co.za", participant.User.Email);
    }

    [Fact]
    public async Task Register_Organiser_GetsAdminAccountAndNextOrganiserNumber()
    {
        var response = await PostJson(Anonymous(), "/api/auth/register", new
        {
            email = "events@capetownrc.co.za",
            userName = "CapeTownRC",
            password = "Str0ngPass!",
            role = "Organiser",
            name = "Cape Town Running Club"
        });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var profile = await ReadAsync<UserProfileResponse>(response);
        Assert.Equal(UserRole.Organiser, profile.Role);
        Assert.Equal(AccountType.Admin, profile.AccountType);
        Assert.Equal(1003, profile.OrganiserNumber); // seeded organisers are 1001 and 1002
    }

    [Fact]
    public async Task Register_StoresHashedPassword_NotPlainText()
    {
        await PostJson(Anonymous(), "/api/auth/register", ParticipantRegistration());

        var hash = await Factory.WithDbAsync(db =>
            db.Users.Where(u => u.Email == "lerato@test.co.za").Select(u => u.PasswordHash).SingleAsync());
        Assert.NotEqual("Str0ngPass!", hash);
        Assert.DoesNotContain("Str0ngPass!", hash);
        Assert.True(hash.Length > 40);
    }

    [Fact]
    public async Task Register_DuplicateEmail_Returns409()
    {
        var response = await PostJson(Anonymous(), "/api/auth/register",
            ParticipantRegistration(email: TestData.ParticipantEmail, userName: "BrandNewName"));

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task Register_DuplicateUserName_Returns409()
    {
        var response = await PostJson(Anonymous(), "/api/auth/register",
            ParticipantRegistration(userName: "ThandiRuns"));

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Theory]
    [InlineData("not-an-email", "Str0ngPass!", "Participant")]
    [InlineData("valid@test.co.za", "short", "Participant")]
    [InlineData("valid@test.co.za", "Str0ngPass!", "Admin")]
    public async Task Register_InvalidDetails_Returns400(string email, string password, string role)
    {
        var response = await PostJson(Anonymous(), "/api/auth/register", new
        {
            email,
            userName = "ValidUser",
            password,
            role,
            name = "Valid Name"
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Register_OrganiserWithPremiumAccount_Returns400()
    {
        var response = await PostJson(Anonymous(), "/api/auth/register", new
        {
            email = "club@test.co.za",
            userName = "SomeClub",
            password = "Str0ngPass!",
            role = "Organiser",
            name = "Some Club",
            accountType = "Premium"
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Login_ValidCredentials_Returns200_AndStartsSession()
    {
        var client = Anonymous();

        var login = await PostJson(client, "/api/auth/login",
            new LoginRequest { Email = TestData.ParticipantEmail, Password = TestData.Password });

        Assert.Equal(HttpStatusCode.OK, login.StatusCode);
        Assert.Contains(login.Headers.GetValues("Set-Cookie"), c => c.StartsWith(".RaceDay.Session"));

        var me = await client.GetAsync("/api/auth/me");
        Assert.Equal(HttpStatusCode.OK, me.StatusCode);
        var profile = await ReadAsync<UserProfileResponse>(me);
        Assert.Equal(Data.ParticipantId, profile.ProfileId);
        Assert.Equal(UserRole.Participant, profile.Role);
    }

    [Fact]
    public async Task Login_EmailIsCaseInsensitive()
    {
        var response = await PostJson(Anonymous(), "/api/auth/login",
            new LoginRequest { Email = TestData.ParticipantEmail.ToUpperInvariant(), Password = TestData.Password });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Login_WrongPassword_Returns401()
    {
        var response = await PostJson(Anonymous(), "/api/auth/login",
            new LoginRequest { Email = TestData.ParticipantEmail, Password = "WrongPassword!" });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Login_UnknownEmail_Returns401()
    {
        var response = await PostJson(Anonymous(), "/api/auth/login",
            new LoginRequest { Email = "nobody@test.co.za", Password = TestData.Password });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Login_MissingPassword_Returns400()
    {
        var response = await PostJson(Anonymous(), "/api/auth/login", new { email = TestData.ParticipantEmail });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task RegisteredUser_CanLogInWithNewPassword()
    {
        await PostJson(Anonymous(), "/api/auth/register", ParticipantRegistration());

        var response = await PostJson(Anonymous(), "/api/auth/login",
            new LoginRequest { Email = "lerato@test.co.za", Password = "Str0ngPass!" });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Logout_ClearsSession_SoProtectedEndpointsReturn401()
    {
        var client = await AsParticipant();

        var logout = await client.PostAsync("/api/auth/logout", null);
        var me = await client.GetAsync("/api/auth/me");

        Assert.Equal(HttpStatusCode.NoContent, logout.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, me.StatusCode);
    }

    [Fact]
    public async Task Me_WithoutSession_Returns401()
    {
        var response = await Anonymous().GetAsync("/api/auth/me");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}
