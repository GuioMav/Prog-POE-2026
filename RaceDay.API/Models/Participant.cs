namespace RaceDay.API.Models;

/// <summary>Participant profile linked one-to-one with a <see cref="User"/>.</summary>
public class Participant
{
    public int ParticipantId { get; set; }
    public string ParticipantName { get; set; } = string.Empty;

    /// <summary>Optional; lets organisers check age categories such as Under 20.</summary>
    public DateOnly? DateOfBirth { get; set; }

    public int UsersId { get; set; }
    public User User { get; set; } = null!;

    public ICollection<Enrollment> Enrollments { get; set; } = new List<Enrollment>();
    public ICollection<Result> Results { get; set; } = new List<Result>();
}
