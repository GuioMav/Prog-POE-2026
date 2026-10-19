namespace RaceDay.API.Models;

/// <summary>A run, walk or cycle event created by an organiser.</summary>
public class Event
{
    public int EventId { get; set; }
    public string EventName { get; set; } = string.Empty;
    public DateOnly EventDate { get; set; }
    public string? Description { get; set; }
    public string Location { get; set; } = string.Empty;

    /// <summary>Main route distance in kilometres.</summary>
    public decimal DistanceKm { get; set; }

    public EventType EventType { get; set; }

    /// <summary>Entry fee in Rand; copied onto each enrollment as its price.</summary>
    public decimal EntryFee { get; set; }

    public int OrganiserId { get; set; }
    public Organiser Organiser { get; set; } = null!;

    public ICollection<Category> Categories { get; set; } = new List<Category>();
    public ICollection<Enrollment> Enrollments { get; set; } = new List<Enrollment>();
    public ICollection<Result> Results { get; set; } = new List<Result>();
}
