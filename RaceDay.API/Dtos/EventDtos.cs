using System.ComponentModel.DataAnnotations;
using System.Linq.Expressions;
using RaceDay.API.Models;

namespace RaceDay.API.Dtos;

/// <summary>Fields an organiser supplies when creating or updating an event.</summary>
public class EventRequest
{
    /// <example>Soweto Sunrise 10K</example>
    [Required, StringLength(100, MinimumLength = 3)]
    public string EventName { get; set; } = string.Empty;

    /// <example>2026-12-06</example>
    [Required]
    public DateOnly? EventDate { get; set; }

    /// <example>A flat community road race through Orlando and Diepkloof.</example>
    [MaxLength(500)]
    public string? Description { get; set; }

    /// <example>Orlando Stadium, Soweto</example>
    [Required, StringLength(100, MinimumLength = 2)]
    public string Location { get; set; } = string.Empty;

    /// <summary>Main route distance in kilometres.</summary>
    /// <example>10</example>
    [Range(0.1, 1000)]
    public decimal DistanceKm { get; set; }

    /// <summary>Run, Walk or Cycle.</summary>
    [Required]
    public EventType? EventType { get; set; }

    /// <summary>Entry fee in Rand (0 for free events).</summary>
    /// <example>150</example>
    [Range(0, 100000)]
    public decimal EntryFee { get; set; }
}

/// <summary>An event as returned by the API.</summary>
public class EventResponse
{
    public int EventId { get; set; }
    public string EventName { get; set; } = string.Empty;
    public DateOnly EventDate { get; set; }
    public string? Description { get; set; }
    public string Location { get; set; } = string.Empty;
    public decimal DistanceKm { get; set; }
    public EventType EventType { get; set; }
    public decimal EntryFee { get; set; }
    public int OrganiserId { get; set; }
    public string OrganiserName { get; set; } = string.Empty;
    public int CategoryCount { get; set; }

    /// <summary>Number of entries that are not cancelled.</summary>
    public int EnrollmentCount { get; set; }

    /// <summary>EF-translatable projection so list queries select only the needed columns.</summary>
    public static readonly Expression<Func<Event, EventResponse>> Projection = e => new EventResponse
    {
        EventId = e.EventId,
        EventName = e.EventName,
        EventDate = e.EventDate,
        Description = e.Description,
        Location = e.Location,
        DistanceKm = e.DistanceKm,
        EventType = e.EventType,
        EntryFee = e.EntryFee,
        OrganiserId = e.OrganiserId,
        OrganiserName = e.Organiser.OrganisationName,
        CategoryCount = e.Categories.Count,
        EnrollmentCount = e.Enrollments.Count(en => en.Status != EnrollmentStatus.Cancelled)
    };
}

/// <summary>Full event information including its categories.</summary>
public class EventDetailResponse : EventResponse
{
    public List<CategoryResponse> Categories { get; set; } = [];
}
