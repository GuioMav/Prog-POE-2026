using Microsoft.AspNetCore.Identity;
using RaceDay.API.Data;
using RaceDay.API.Models;

namespace RaceDay.API.Tests.Infrastructure;

/// <summary>
/// Known accounts and events added to every API test database:
/// two organisers, two participants, an upcoming event and a past event
/// (both owned by the first organiser) with categories.
/// </summary>
public class TestData
{
    public const string Password = "Password123!";

    public const string OrganiserEmail = "organiser@test.co.za";
    public const string OtherOrganiserEmail = "other.organiser@test.co.za";
    public const string ParticipantEmail = "participant@test.co.za";
    public const string OtherParticipantEmail = "other.participant@test.co.za";

    public int OrganiserId { get; private set; }
    public int OtherOrganiserId { get; private set; }
    public int ParticipantId { get; private set; }
    public int ParticipantUserId { get; private set; }
    public int OtherParticipantId { get; private set; }

    public int UpcomingEventId { get; private set; }
    public int Upcoming10kCategoryId { get; private set; }
    public int UpcomingUnder20CategoryId { get; private set; }

    public int PastEventId { get; private set; }
    public int Past21kCategoryId { get; private set; }

    public static DateOnly Today => DateOnly.FromDateTime(DateTime.UtcNow);

    public static async Task<TestData> SeedAsync(RaceDayDbContext db)
    {
        var hasher = new PasswordHasher<User>();

        User NewUser(string email, string userName, UserRole role, AccountType type)
        {
            var user = new User { Email = email, UserName = userName, Role = role, AccountType = type };
            user.PasswordHash = hasher.HashPassword(user, Password);
            return user;
        }

        var organiser = new Organiser
        {
            OrganiserNumber = 1001, OrganisationName = "Soweto Runners Club",
            User = NewUser(OrganiserEmail, "SowetoRunners", UserRole.Organiser, AccountType.Admin)
        };
        var otherOrganiser = new Organiser
        {
            OrganiserNumber = 1002, OrganisationName = "Durban Cycling Club",
            User = NewUser(OtherOrganiserEmail, "DurbanCycling", UserRole.Organiser, AccountType.Admin)
        };
        var participant = new Participant
        {
            ParticipantName = "Thandi Nkosi", DateOfBirth = new DateOnly(1998, 5, 1),
            User = NewUser(ParticipantEmail, "ThandiRuns", UserRole.Participant, AccountType.Standard)
        };
        var otherParticipant = new Participant
        {
            ParticipantName = "Pieter van Wyk",
            User = NewUser(OtherParticipantEmail, "PieterRides", UserRole.Participant, AccountType.Premium)
        };

        var upcoming = new Event
        {
            EventName = "Soweto Sunrise 10K", EventDate = Today.AddDays(30), Location = "Orlando Stadium",
            DistanceKm = 10, EventType = EventType.Run, EntryFee = 80, Organiser = organiser
        };
        var tenK = new Category { CategoryName = "10km", CategoryType = CategoryType.Distance, Event = upcoming };
        var under20 = new Category { CategoryName = "Under 20", CategoryType = CategoryType.Age, Event = upcoming };

        var past = new Event
        {
            EventName = "Two Oceans Warm-up", EventDate = Today.AddDays(-7), Location = "Newlands",
            DistanceKm = 21.1m, EventType = EventType.Run, EntryFee = 150, Organiser = organiser
        };
        var twentyOneK = new Category { CategoryName = "21km", CategoryType = CategoryType.Distance, Event = past };

        db.AddRange(organiser, otherOrganiser, participant, otherParticipant, upcoming, past, tenK, under20, twentyOneK);
        await db.SaveChangesAsync();

        return new TestData
        {
            OrganiserId = organiser.OrganiserId,
            OtherOrganiserId = otherOrganiser.OrganiserId,
            ParticipantId = participant.ParticipantId,
            ParticipantUserId = participant.UsersId,
            OtherParticipantId = otherParticipant.ParticipantId,
            UpcomingEventId = upcoming.EventId,
            Upcoming10kCategoryId = tenK.CategoryId,
            UpcomingUnder20CategoryId = under20.CategoryId,
            PastEventId = past.EventId,
            Past21kCategoryId = twentyOneK.CategoryId
        };
    }
}
