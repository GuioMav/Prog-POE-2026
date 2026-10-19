namespace RaceDay.API.Models;

/// <summary>An age or distance category within an event (e.g. Under 20, Senior, 10km, 21km).</summary>
public class Category
{
    public int CategoryId { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public CategoryType CategoryType { get; set; }

    public int EventId { get; set; }
    public Event Event { get; set; } = null!;

    public ICollection<Enrollment> Enrollments { get; set; } = new List<Enrollment>();
}
