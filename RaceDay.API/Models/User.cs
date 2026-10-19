namespace RaceDay.API.Models;

/// <summary>
/// Login account shared by both roles. Role-specific details live in the
/// <see cref="Organiser"/> or <see cref="Participant"/> table (one-to-one).
/// </summary>
public class User
{
    public int UsersId { get; set; }
    public string Email { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;

    /// <summary>PBKDF2 hash produced by ASP.NET Core's PasswordHasher - never the plain password.</summary>
    public string PasswordHash { get; set; } = string.Empty;

    public UserRole Role { get; set; }
    public AccountType AccountType { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public Organiser? Organiser { get; set; }
    public Participant? Participant { get; set; }
}
