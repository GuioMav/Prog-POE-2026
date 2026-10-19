using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.API.Auth;
using RaceDay.API.Data;
using RaceDay.API.Dtos;
using RaceDay.API.Models;

namespace RaceDay.API.Controllers;

/// <summary>
/// Race results. Organisers capture finish times and positions for their own
/// events once the event has taken place; participants view their own history.
/// </summary>
[Route("api")]
public class ResultsController : RaceDayControllerBase
{
    private readonly RaceDayDbContext _db;

    public ResultsController(RaceDayDbContext db)
    {
        _db = db;
    }

    /// <summary>View the results for an event (leaderboard).</summary>
    /// <param name="eventId">EventId.</param>
    /// <response code="200">Results ordered by finishing position.</response>
    /// <response code="404">Event not found.</response>
    [HttpGet("events/{eventId:int}/results")]
    [ProducesResponseType<List<ResultResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetEventResults(int eventId)
    {
        if (!await _db.Events.AnyAsync(e => e.EventId == eventId))
            return NotFoundProblem("Event not found.");

        var results = await _db.Results.AsNoTracking()
            .Where(r => r.EventId == eventId)
            .OrderBy(r => r.Position)
            .Select(ResultResponse.Projection)
            .ToListAsync();

        return Ok(results);
    }

    /// <summary>Record a participant's finish time and position (Organiser).</summary>
    /// <remarks>
    /// Only for your own events, on or after the event date, and only for participants
    /// with an active (not cancelled) entry. Each participant has one result per event
    /// and each position can only be used once per event.
    /// </remarks>
    /// <response code="201">Result recorded.</response>
    /// <response code="400">Validation failed, the event has not happened yet, or the participant is not entered.</response>
    /// <response code="401">Not logged in.</response>
    /// <response code="403">Not an organiser, or the event belongs to another organiser.</response>
    /// <response code="404">Event not found.</response>
    /// <response code="409">A result already exists for this participant, or the position is taken.</response>
    [HttpPost("results")]
    [SessionAuthorize(UserRole.Organiser)]
    [ProducesResponseType<ResultResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> RecordResult(CreateResultRequest request)
    {
        var eventId = request.EventId!.Value;
        var participantId = request.ParticipantId!.Value;

        var raceEvent = await _db.Events.FindAsync(eventId);
        if (raceEvent is null)
            return NotFoundProblem("Event not found.");
        if (raceEvent.OrganiserId != CurrentProfileId)
            return ForbiddenProblem("You can only record results for events you created.");

        if (raceEvent.EventDate > DateOnly.FromDateTime(DateTime.UtcNow))
        {
            ModelState.AddModelError(nameof(request.EventId), "Results can only be captured once the event has taken place.");
            return ValidationProblem(ModelState);
        }

        var isEntered = await _db.Enrollments.AnyAsync(e =>
            e.EventId == eventId && e.ParticipantId == participantId && e.Status != EnrollmentStatus.Cancelled);
        if (!isEntered)
        {
            ModelState.AddModelError(nameof(request.ParticipantId), "This participant is not entered in the event.");
            return ValidationProblem(ModelState);
        }

        if (await _db.Results.AnyAsync(r => r.EventId == eventId && r.ParticipantId == participantId))
            return ConflictProblem("A result has already been recorded for this participant. Update it instead.");
        if (await PositionTaken(eventId, request.Position, exceptResultId: null))
            return ConflictProblem($"Position {request.Position} has already been given to another participant.");

        var result = new Result
        {
            EventId = eventId,
            ParticipantId = participantId,
            FinishingTime = request.FinishingTime!.Value,
            Position = request.Position
        };

        _db.Results.Add(result);
        await _db.SaveChangesAsync();

        return Created($"/api/events/{eventId}/results", await Project(result.ResultId));
    }

    /// <summary>Correct a recorded result (Organiser).</summary>
    /// <param name="id">ResultId.</param>
    /// <param name="request">New finishing time and position.</param>
    /// <response code="200">Updated result.</response>
    /// <response code="400">Validation failed.</response>
    /// <response code="401">Not logged in.</response>
    /// <response code="403">Not an organiser, or the event belongs to another organiser.</response>
    /// <response code="404">Result not found.</response>
    /// <response code="409">The position is already taken.</response>
    [HttpPut("results/{id:int}")]
    [SessionAuthorize(UserRole.Organiser)]
    [ProducesResponseType<ResultResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> UpdateResult(int id, ResultValues request)
    {
        var result = await _db.Results.Include(r => r.Event).SingleOrDefaultAsync(r => r.ResultId == id);
        if (result is null)
            return NotFoundProblem("Result not found.");
        if (result.Event.OrganiserId != CurrentProfileId)
            return ForbiddenProblem("You can only change results for events you created.");
        if (await PositionTaken(result.EventId, request.Position, exceptResultId: id))
            return ConflictProblem($"Position {request.Position} has already been given to another participant.");

        result.FinishingTime = request.FinishingTime!.Value;
        result.Position = request.Position;
        await _db.SaveChangesAsync();

        return Ok(await Project(id));
    }

    /// <summary>Delete a recorded result (Organiser).</summary>
    /// <param name="id">ResultId.</param>
    /// <response code="204">Result deleted.</response>
    /// <response code="401">Not logged in.</response>
    /// <response code="403">Not an organiser, or the event belongs to another organiser.</response>
    /// <response code="404">Result not found.</response>
    [HttpDelete("results/{id:int}")]
    [SessionAuthorize(UserRole.Organiser)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteResult(int id)
    {
        var result = await _db.Results.Include(r => r.Event).SingleOrDefaultAsync(r => r.ResultId == id);
        if (result is null)
            return NotFoundProblem("Result not found.");
        if (result.Event.OrganiserId != CurrentProfileId)
            return ForbiddenProblem("You can only delete results for events you created.");

        _db.Results.Remove(result);
        await _db.SaveChangesAsync();

        return NoContent();
    }

    /// <summary>View a participant's race history (your own only).</summary>
    /// <remarks>Includes event, category, finish time, position and total finishers.</remarks>
    /// <param name="participantId">ParticipantId of the logged-in participant.</param>
    /// <response code="200">Results, most recent event first.</response>
    /// <response code="401">Not logged in.</response>
    /// <response code="403">Not a participant, or another participant's id.</response>
    [HttpGet("participants/{participantId:int}/results")]
    [SessionAuthorize(UserRole.Participant)]
    [ProducesResponseType<List<ResultResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetParticipantResults(int participantId)
    {
        if (participantId != CurrentProfileId)
            return ForbiddenProblem("You can only view your own results.");

        var results = await _db.Results.AsNoTracking()
            .Where(r => r.ParticipantId == participantId)
            .OrderByDescending(r => r.Event.EventDate)
            .Select(ResultResponse.Projection)
            .ToListAsync();

        return Ok(results);
    }

    private Task<bool> PositionTaken(int eventId, int position, int? exceptResultId) =>
        _db.Results.AnyAsync(r => r.EventId == eventId && r.Position == position && r.ResultId != exceptResultId);

    private Task<ResultResponse> Project(int resultId) => _db.Results.AsNoTracking()
        .Where(r => r.ResultId == resultId)
        .Select(ResultResponse.Projection)
        .SingleAsync();
}
