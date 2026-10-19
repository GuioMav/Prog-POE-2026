using System.ComponentModel.DataAnnotations;
using RaceDay.API.Models;

namespace RaceDay.API.Dtos;

/// <summary>A user's profile as returned by the API (never includes the password hash).</summary>
public class UserProfileResponse
{
    public int UsersId { get; set; }
    public string Email { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public UserRole Role { get; set; }
    public AccountType AccountType { get; set; }

    /// <summary>OrganiserId for organisers, ParticipantId for participants.</summary>
    public int ProfileId { get; set; }

    /// <summary>Participant's full name or organiser's organisation name.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Organisers only.</summary>
    public int? OrganiserNumber { get; set; }

    /// <summary>Participants only.</summary>
    public DateOnly? DateOfBirth { get; set; }

    public DateTime CreatedAt { get; set; }

    /// <summary>Builds the response from a user whose Organiser/Participant row is loaded.</summary>
    public static UserProfileResponse From(User user) => new()
    {
        UsersId = user.UsersId,
        Email = user.Email,
        UserName = user.UserName,
        Role = user.Role,
        AccountType = user.AccountType,
        ProfileId = user.Organiser?.OrganiserId ?? user.Participant?.ParticipantId ?? 0,
        Name = user.Organiser?.OrganisationName ?? user.Participant?.ParticipantName ?? string.Empty,
        OrganiserNumber = user.Organiser?.OrganiserNumber,
        DateOfBirth = user.Participant?.DateOfBirth,
        CreatedAt = user.CreatedAt
    };
}

/// <summary>Profile fields a user may change. Role and account type cannot be changed here.</summary>
public class UpdateProfileRequest
{
    /// <example>thandi.nkosi@example.com</example>
    [Required, EmailAddress, MaxLength(100)]
    public string Email { get; set; } = string.Empty;

    /// <example>ThandiRuns</example>
    [Required, StringLength(50, MinimumLength = 3)]
    public string UserName { get; set; } = string.Empty;

    /// <summary>Participant: full name. Organiser: organisation name.</summary>
    /// <example>Thandi Nkosi</example>
    [Required, StringLength(100, MinimumLength = 2)]
    public string Name { get; set; } = string.Empty;

    /// <summary>Participants only; ignored for organisers.</summary>
    public DateOnly? DateOfBirth { get; set; }
}
