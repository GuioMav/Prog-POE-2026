using System.ComponentModel.DataAnnotations;
using System.Linq.Expressions;
using RaceDay.API.Models;

namespace RaceDay.API.Dtos;

/// <summary>Event and category a participant wants to enter.</summary>
public class CreateEnrollmentRequest
{
    /// <example>1</example>
    [Required, Range(1, int.MaxValue)]
    public int? EventId { get; set; }

    /// <summary>Must be one of the event's categories.</summary>
    /// <example>2</example>
    [Required, Range(1, int.MaxValue)]
    public int? CategoryId { get; set; }
}

/// <summary>New status for an enrollment.</summary>
public class UpdateEnrollmentStatusRequest
{
    /// <summary>Pending, Confirmed or Cancelled. Participants may only cancel.</summary>
    [Required]
    public EnrollmentStatus? Status { get; set; }
}

/// <summary>An enrollment with the event, category and participant names filled in.</summary>
public class EnrollmentResponse
{
    public int EnrollmentId { get; set; }
    public int EventId { get; set; }
    public string EventName { get; set; } = string.Empty;
    public DateOnly EventDate { get; set; }
    public string Location { get; set; } = string.Empty;
    public int CategoryId { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public int ParticipantId { get; set; }
    public string ParticipantName { get; set; } = string.Empty;
    public DateOnly EnrollmentDate { get; set; }
    public EnrollmentStatus Status { get; set; }
    public decimal Price { get; set; }

    public static readonly Expression<Func<Enrollment, EnrollmentResponse>> Projection = e => new EnrollmentResponse
    {
        EnrollmentId = e.EnrollmentId,
        EventId = e.EventId,
        EventName = e.Event.EventName,
        EventDate = e.Event.EventDate,
        Location = e.Event.Location,
        CategoryId = e.CategoryId,
        CategoryName = e.Category.CategoryName,
        ParticipantId = e.ParticipantId,
        ParticipantName = e.Participant.ParticipantName,
        EnrollmentDate = e.EnrollmentDate,
        Status = e.Status,
        Price = e.Price
    };
}
