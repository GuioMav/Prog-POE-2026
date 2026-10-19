namespace RaceDay.API.Models;

/// <summary>Finish time and position captured by the organiser after an event.</summary>
public class Result
{
    public int ResultId { get; set; }

    public int ParticipantId { get; set; }
    public Participant Participant { get; set; } = null!;

    public int EventId { get; set; }
    public Event Event { get; set; } = null!;

    public TimeSpan FinishingTime { get; set; }
    public int Position { get; set; }
}
