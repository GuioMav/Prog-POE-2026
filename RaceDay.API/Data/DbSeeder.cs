using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using RaceDay.API.Models;

namespace RaceDay.API.Data;

/// <summary>
/// Fills an empty database with the same sample data as docs/raceday.sql so the
/// API can be demonstrated straight away. Every seeded account uses the password
/// <see cref="SeedPassword"/>.
/// </summary>
public static class DbSeeder
{
    public const string SeedPassword = "Password123!";

    public static async Task SeedAsync(RaceDayDbContext db, IPasswordHasher<User> hasher)
    {
        if (await db.Users.AnyAsync())
            return;

        User NewUser(string email, string userName, UserRole role, AccountType type)
        {
            var user = new User { Email = email, UserName = userName, Role = role, AccountType = type };
            user.PasswordHash = hasher.HashPassword(user, SeedPassword);
            return user;
        }

        Participant NewParticipant(string email, string userName, string name, AccountType type, DateOnly dob) =>
            new()
            {
                ParticipantName = name,
                DateOfBirth = dob,
                User = NewUser(email, userName, UserRole.Participant, type)
            };

        Organiser NewOrganiser(string email, string userName, int number, string organisation) =>
            new()
            {
                OrganiserNumber = number,
                OrganisationName = organisation,
                User = NewUser(email, userName, UserRole.Organiser, AccountType.Admin)
            };

        // Organisers
        var david = NewOrganiser("david@example.com", "DavidEvents", 1001, "Spring City Athletics");
        var emma = NewOrganiser("emma@example.com", "EmmaEvents", 1002, "Cape Trail Runners");
        var henry = NewOrganiser("henry@example.com", "HenryEvents", 1003, "Winter Charity Trust");

        // Participants
        var alice = NewParticipant("alice@example.com", "AliceRunner", "Alice Johnson", AccountType.Standard, new DateOnly(1995, 4, 12));
        var bob = NewParticipant("bob@example.com", "BobRunner", "Bob Smith", AccountType.Standard, new DateOnly(1988, 9, 3));
        var carol = NewParticipant("carol@example.com", "CarolRunner", "Carol Williams", AccountType.Premium, new DateOnly(2001, 1, 27));
        var frank = NewParticipant("frank@example.com", "FrankRunner", "Frank Brown", AccountType.Standard, new DateOnly(1979, 6, 15));
        var grace = NewParticipant("grace@example.com", "GraceRunner", "Grace Wilson", AccountType.Premium, new DateOnly(1999, 11, 8));
        var isabel = NewParticipant("isabel@example.com", "IsabelRunner", "Isabel Davis", AccountType.Standard, new DateOnly(2008, 2, 19));
        var james = NewParticipant("james@example.com", "JamesRunner", "James Miller", AccountType.Standard, new DateOnly(1992, 7, 30));

        // Events
        var spring = new Event
        {
            EventName = "Spring City Run", EventDate = new DateOnly(2026, 9, 20), Location = "Central Park",
            Description = "A community road running event for all fitness levels.",
            DistanceKm = 21.1m, EventType = EventType.Run, EntryFee = 150m, Organiser = david
        };
        var beach = new Event
        {
            EventName = "Summer Beach Race", EventDate = new DateOnly(2026, 10, 11), Location = "Brighton Beach",
            Description = "A beach running event featuring multiple race distances.",
            DistanceKm = 21.1m, EventType = EventType.Run, EntryFee = 250m, Organiser = david
        };
        var mountain = new Event
        {
            EventName = "Autumn Mountain Challenge", EventDate = new DateOnly(2026, 10, 25), Location = "Highlands",
            Description = "A challenging trail running event through the Highlands.",
            DistanceKm = 15m, EventType = EventType.Run, EntryFee = 300m, Organiser = emma
        };
        var charity = new Event
        {
            EventName = "Winter Charity Walk", EventDate = new DateOnly(2026, 12, 6), Location = "City Stadium",
            Description = "A charity walk supporting local community projects.",
            DistanceKm = 21.1m, EventType = EventType.Walk, EntryFee = 100m, Organiser = henry
        };
        var cape = new Event
        {
            EventName = "Cape Fitness Ride", EventDate = new DateOnly(2026, 11, 8), Location = "Green Point",
            Description = "A fitness-focused cycle race for recreational and competitive riders.",
            DistanceKm = 50m, EventType = EventType.Cycle, EntryFee = 275m, Organiser = emma
        };
        var newYear = new Event
        {
            EventName = "New Year Road Race", EventDate = new DateOnly(2027, 1, 10), Location = "Main Street",
            Description = "A new year road race welcoming runners of different abilities.",
            DistanceKm = 21.1m, EventType = EventType.Run, EntryFee = 120m, Organiser = david
        };

        Category Cat(Event e, string name, CategoryType type = CategoryType.Distance)
        {
            var c = new Category { CategoryName = name, CategoryType = type, Event = e };
            e.Categories.Add(c);
            return c;
        }

        var spring5 = Cat(spring, "5K Fun Run");
        var spring10 = Cat(spring, "10K Race");
        var spring21 = Cat(spring, "21K Half Marathon");
        var beach5 = Cat(beach, "5K Beach Run");
        var beach10 = Cat(beach, "10K Beach Race");
        var beach21 = Cat(beach, "Half Marathon");
        Cat(mountain, "5K Mountain Trail");
        var mountain10 = Cat(mountain, "10K Mountain Trail");
        var mountain15 = Cat(mountain, "15K Mountain Challenge");
        var charity5 = Cat(charity, "5K Charity Walk");
        Cat(charity, "10K Charity Walk");
        Cat(charity, "21K Charity Walk");
        var charityU20 = Cat(charity, "Under 20", CategoryType.Age);
        Cat(charity, "Senior", CategoryType.Age);
        var cape25 = Cat(cape, "25K Fitness Ride");
        var cape50 = Cat(cape, "50K Fitness Ride");
        Cat(cape, "Veterans 50+", CategoryType.Age);
        var ny5 = Cat(newYear, "5K Road Race");
        var ny10 = Cat(newYear, "10K Road Race");
        Cat(newYear, "21K New Year Race");

        Enrollment Enter(Participant p, Category c, int day) => new()
        {
            Participant = p,
            Event = c.Event,
            Category = c,
            EnrollmentDate = new DateOnly(2026, 8, day),
            Status = EnrollmentStatus.Confirmed,
            Price = c.Event.EntryFee
        };

        db.Enrollments.AddRange(
            Enter(alice, spring21, 25), Enter(bob, spring21, 26), Enter(carol, spring10, 27), Enter(frank, spring5, 28),
            Enter(alice, beach21, 25), Enter(bob, beach21, 26), Enter(carol, beach21, 27), Enter(grace, beach10, 28),
            Enter(bob, mountain15, 27), Enter(frank, mountain10, 28), Enter(grace, mountain15, 29),
            Enter(alice, charity5, 25), Enter(grace, charity5, 28), Enter(isabel, charityU20, 29),
            Enter(carol, cape50, 27), Enter(frank, cape25, 28), Enter(james, cape50, 29),
            Enter(alice, ny10, 25), Enter(isabel, ny5, 29), Enter(james, ny10, 30));

        Result Finish(Participant p, Event e, string time, int position) => new()
        {
            Participant = p,
            Event = e,
            FinishingTime = TimeSpan.Parse(time),
            Position = position
        };

        db.Results.AddRange(
            Finish(alice, spring, "01:38:35", 1), Finish(bob, spring, "01:51:42", 2), Finish(carol, spring, "00:54:18", 3),
            Finish(alice, beach, "01:52:20", 1), Finish(bob, beach, "02:01:45", 2), Finish(carol, beach, "02:08:30", 3));

        await db.SaveChangesAsync();
    }
}
