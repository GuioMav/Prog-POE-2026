using System.ComponentModel.DataAnnotations;
using RaceDay.API.Models;

namespace RaceDay.API.Dtos;

/// <summary>Details needed to create an Organiser or Participant account.</summary>
public class RegisterRequest : IValidatableObject
{
    /// <example>thandi@example.com</example>
    [Required, EmailAddress, MaxLength(100)]
    public string Email { get; set; } = string.Empty;

    /// <example>ThandiRuns</example>
    [Required, StringLength(50, MinimumLength = 3)]
    public string UserName { get; set; } = string.Empty;

    /// <summary>At least 8 characters. Stored only as a salted hash.</summary>
    /// <example>Password123!</example>
    [Required, StringLength(100, MinimumLength = 8)]
    public string Password { get; set; } = string.Empty;

    /// <summary>Organiser or Participant.</summary>
    [Required]
    public UserRole? Role { get; set; }

    /// <summary>Participant: full name. Organiser: organisation name (e.g. "Soweto Runners Club").</summary>
    /// <example>Thandi Nkosi</example>
    [Required, StringLength(100, MinimumLength = 2)]
    public string Name { get; set; } = string.Empty;

    /// <summary>Participants only (optional). Used for age categories.</summary>
    public DateOnly? DateOfBirth { get; set; }

    /// <summary>Participants only: Standard (default) or Premium. Organisers are always Admin.</summary>
    public AccountType? AccountType { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (Role == UserRole.Organiser && (DateOfBirth is not null || AccountType is not null and not Models.AccountType.Admin))
        {
            yield return new ValidationResult(
                "Organisers cannot set a date of birth or a participant account type.",
                [nameof(DateOfBirth), nameof(AccountType)]);
        }

        if (Role == UserRole.Participant && AccountType == Models.AccountType.Admin)
        {
            yield return new ValidationResult("Participants can only be Standard or Premium.", [nameof(AccountType)]);
        }

        if (DateOfBirth is { } dob && dob > DateOnly.FromDateTime(DateTime.UtcNow))
        {
            yield return new ValidationResult("Date of birth cannot be in the future.", [nameof(DateOfBirth)]);
        }
    }
}

/// <summary>Login credentials.</summary>
public class LoginRequest
{
    /// <example>alice@example.com</example>
    [Required, EmailAddress]
    public string Email { get; set; } = string.Empty;

    /// <example>Password123!</example>
    [Required]
    public string Password { get; set; } = string.Empty;
}
