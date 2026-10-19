using System.ComponentModel.DataAnnotations;
using System.Linq.Expressions;
using RaceDay.API.Models;

namespace RaceDay.API.Dtos;

/// <summary>Finish time and position for a participant.</summary>
public class ResultValues : IValidatableObject
{
    /// <summary>Finishing time as hh:mm:ss.</summary>
    /// <example>00:47:12</example>
    [Required]
    public TimeSpan? FinishingTime { get; set; }

    /// <summary>Finishing position in the event (1 = winner).</summary>
    /// <example>3</example>
    [Range(1, 100000)]
    public int Position { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (FinishingTime is { } t && (t <= TimeSpan.Zero || t >= TimeSpan.FromDays(1)))
            yield return new ValidationResult("Finishing time must be between 00:00:01 and 23:59:59.", [nameof(FinishingTime)]);
    }
}

/// <summary>Result to record for an enrolled participant.</summary>
public class CreateResultRequest : ResultValues
{
    /// <example>1</example>
    [Required, Range(1, int.MaxValue)]
    public int? ParticipantId { get; set; }

    /// <example>1</example>
    [Required, Range(1, int.MaxValue)]
    public int? EventId { get; set; }
}

/// <summary>A result with event, participant and category details.</summary>
public class ResultResponse
{
    public int ResultId { get; set; }
    public int EventId { get; set; }
    public string EventName { get; set; } = string.Empty;
    public DateOnly EventDate { get; set; }
    public int ParticipantId { get; set; }
    public string ParticipantName { get; set; } = string.Empty;

    /// <summary>The category the participant entered.</summary>
    public string? CategoryName { get; set; }

    public TimeSpan FinishingTime { get; set; }
    public int Position { get; set; }

    /// <summary>Number of finishers recorded for the event (for "47th out of 312").</summary>
    public int TotalFinishers { get; set; }

    public static readonly Expression<Func<Result, ResultResponse>> Projection = r => new ResultResponse
    {
        ResultId = r.ResultId,
        EventId = r.EventId,
        EventName = r.Event.EventName,
        EventDate = r.Event.EventDate,
        ParticipantId = r.ParticipantId,
        ParticipantName = r.Participant.ParticipantName,
        CategoryName = r.Event.Enrollments
            .Where(en => en.ParticipantId == r.ParticipantId)
            .Select(en => en.Category.CategoryName)
            .FirstOrDefault(),
        FinishingTime = r.FinishingTime,
        Position = r.Position,
        TotalFinishers = r.Event.Results.Count
    };
}
