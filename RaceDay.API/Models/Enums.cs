namespace RaceDay.API.Models;

// Enums are stored as strings in the database (see RaceDayDbContext) so the
// tables stay readable in SSMS and match the VARCHAR columns from Part 1.

/// <summary>The two roles a RaceDay account can have.</summary>
public enum UserRole
{
    Organiser,
    Participant
}

/// <summary>Account tier. Organisers are always Admin; Participants are Standard or Premium.</summary>
public enum AccountType
{
    Standard,
    Premium,
    Admin
}

/// <summary>The kind of road event being held.</summary>
public enum EventType
{
    Run,
    Walk,
    Cycle
}

/// <summary>Whether a category groups entrants by age (e.g. Under 20) or by distance (e.g. 10km).</summary>
public enum CategoryType
{
    Age,
    Distance
}

/// <summary>Lifecycle of an event entry.</summary>
public enum EnrollmentStatus
{
    Pending,
    Confirmed,
    Cancelled
}
