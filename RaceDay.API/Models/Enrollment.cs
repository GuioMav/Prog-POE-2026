namespace RaceDay.API.Models;

/// <summary>
/// Links a participant to an event and the category they selected.
/// A participant can enter a given event only once.
/// </summary>
public class Enrollment
{
    public int EnrollmentId { get; set; }

    public int ParticipantId { get; set; }
    public Participant Participant { get; set; } = null!;

    public int EventId { get; set; }
    public Event Event { get; set; } = null!;

    public int CategoryId { get; set; }
    public Category Category { get; set; } = null!;

    public DateOnly EnrollmentDate { get; set; }
    public EnrollmentStatus Status { get; set; } = EnrollmentStatus.Confirmed;
    public decimal Price { get; set; }
}
