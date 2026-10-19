namespace RaceDay.API.Models;

/// <summary>Organiser profile linked one-to-one with a <see cref="User"/>.</summary>
public class Organiser
{
    public int OrganiserId { get; set; }

    /// <summary>Public reference number shown to participants (e.g. 1001).</summary>
    public int OrganiserNumber { get; set; }

    public string OrganisationName { get; set; } = string.Empty;

    public int UsersId { get; set; }
    public User User { get; set; } = null!;

    public ICollection<Event> Events { get; set; } = new List<Event>();
}
