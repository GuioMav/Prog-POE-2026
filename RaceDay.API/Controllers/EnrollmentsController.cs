using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.API.Auth;
using RaceDay.API.Data;
using RaceDay.API.Dtos;
using RaceDay.API.Models;

namespace RaceDay.API.Controllers;

/// <summary>
/// Event entries. Participants enter an event by choosing one of its categories;
/// organisers see and manage the entries for their own events.
/// </summary>
[Route("api")]
[ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
[ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
public class EnrollmentsController : RaceDayControllerBase
{
    private readonly RaceDayDbContext _db;

    public EnrollmentsController(RaceDayDbContext db)
    {
        _db = db;
    }

    /// <summary>Enter an event by selecting a category (Participant).</summary>
    /// <remarks>
    /// Records the link between the logged-in participant, the event and the chosen category.
    /// The price is taken from the event's entry fee. Each participant can enter an event once;
    /// a previously cancelled entry is re-activated instead.
    /// </remarks>
    /// <response code="201">Entry recorded with status Confirmed.</response>
    /// <response code="400">Validation failed, the category is not part of the event, or the event has already taken place.</response>
    /// <response code="401">Not logged in.</response>
    /// <response code="403">Logged in as an organiser.</response>
    /// <response code="404">Event or category not found.</response>
    /// <response code="409">Already entered in this event.</response>
    [HttpPost("enrollments")]
    [SessionAuthorize(UserRole.Participant)]
    [ProducesResponseType<EnrollmentResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Enrol(CreateEnrollmentRequest request)
    {
        var participantId = CurrentProfileId!.Value;
        var eventId = request.EventId!.Value;
        var categoryId = request.CategoryId!.Value;

        var raceEvent = await _db.Events.FindAsync(eventId);
        if (raceEvent is null)
            return NotFoundProblem("Event not found.");

        var category = await _db.Categories.FindAsync(categoryId);
        if (category is null)
            return NotFoundProblem("Category not found.");

        if (category.EventId != eventId)
        {
            ModelState.AddModelError(nameof(request.CategoryId), "The selected category does not belong to this event.");
            return ValidationProblem(ModelState);
        }

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        if (raceEvent.EventDate < today)
        {
            ModelState.AddModelError(nameof(request.EventId), "This event has already taken place.");
            return ValidationProblem(ModelState);
        }

        var enrollment = await _db.Enrollments
            .SingleOrDefaultAsync(e => e.ParticipantId == participantId && e.EventId == eventId);

        if (enrollment is { Status: not EnrollmentStatus.Cancelled })
            return ConflictProblem("You are already entered in this event.");

        if (enrollment is null)
        {
            enrollment = new Enrollment { ParticipantId = participantId, EventId = eventId };
            _db.Enrollments.Add(enrollment);
        }

        enrollment.CategoryId = categoryId;
        enrollment.EnrollmentDate = today;
        enrollment.Status = EnrollmentStatus.Confirmed;
        enrollment.Price = raceEvent.EntryFee;
        await _db.SaveChangesAsync();

        var response = await Project(enrollment.EnrollmentId);
        return CreatedAtAction(nameof(GetEnrollment), new { id = enrollment.EnrollmentId }, response);
    }

    /// <summary>View one enrollment.</summary>
    /// <remarks>Visible to the participant who owns it and to the organiser of the event.</remarks>
    /// <param name="id">EnrollmentId.</param>
    /// <response code="200">The enrollment.</response>
    /// <response code="401">Not logged in.</response>
    /// <response code="403">Neither the entrant nor the event's organiser.</response>
    /// <response code="404">Enrollment not found.</response>
    [HttpGet("enrollments/{id:int}")]
    [SessionAuthorize]
    [ProducesResponseType<EnrollmentResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetEnrollment(int id)
    {
        var enrollment = await _db.Enrollments.AsNoTracking().Include(e => e.Event)
            .SingleOrDefaultAsync(e => e.EnrollmentId == id);
        if (enrollment is null)
            return NotFoundProblem("Enrollment not found.");
        if (!IsEntrant(enrollment) && !IsEventOrganiser(enrollment))
            return ForbiddenProblem("You can only view your own enrollments or those for your events.");

        return Ok(await Project(id));
    }

    /// <summary>View a participant's enrollments (your own only).</summary>
    /// <param name="participantId">ParticipantId of the logged-in participant.</param>
    /// <response code="200">Enrollments, most recent event first.</response>
    /// <response code="401">Not logged in.</response>
    /// <response code="403">Not a participant, or another participant's id.</response>
    [HttpGet("participants/{participantId:int}/enrollments")]
    [SessionAuthorize(UserRole.Participant)]
    [ProducesResponseType<List<EnrollmentResponse>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetParticipantEnrollments(int participantId)
    {
        if (participantId != CurrentProfileId)
            return ForbiddenProblem("You can only view your own enrollments.");

        var enrollments = await _db.Enrollments.AsNoTracking()
            .Where(e => e.ParticipantId == participantId)
            .OrderByDescending(e => e.Event.EventDate)
            .Select(EnrollmentResponse.Projection)
            .ToListAsync();

        return Ok(enrollments);
    }

    /// <summary>View everyone enrolled in one of your events (Organiser).</summary>
    /// <param name="eventId">EventId.</param>
    /// <response code="200">Enrollments with participant name, category and status.</response>
    /// <response code="401">Not logged in.</response>
    /// <response code="403">Not an organiser, or the event belongs to another organiser.</response>
    /// <response code="404">Event not found.</response>
    [HttpGet("events/{eventId:int}/enrollments")]
    [SessionAuthorize(UserRole.Organiser)]
    [ProducesResponseType<List<EnrollmentResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetEventEnrollments(int eventId)
    {
        var raceEvent = await _db.Events.FindAsync(eventId);
        if (raceEvent is null)
            return NotFoundProblem("Event not found.");
        if (raceEvent.OrganiserId != CurrentProfileId)
            return ForbiddenProblem("You can only view enrollments for events you created.");

        var enrollments = await _db.Enrollments.AsNoTracking()
            .Where(e => e.EventId == eventId)
            .OrderBy(e => e.Category.CategoryName)
            .ThenBy(e => e.Participant.ParticipantName)
            .Select(EnrollmentResponse.Projection)
            .ToListAsync();

        return Ok(enrollments);
    }

    /// <summary>Change an enrollment's status.</summary>
    /// <remarks>
    /// The event's organiser can set any status (Pending, Confirmed, Cancelled).
    /// The participant who owns the entry may only set it to Cancelled.
    /// </remarks>
    /// <param name="id">EnrollmentId.</param>
    /// <param name="request">The new status.</param>
    /// <response code="200">Updated enrollment.</response>
    /// <response code="400">Validation failed.</response>
    /// <response code="401">Not logged in.</response>
    /// <response code="403">Not allowed to change this enrollment, or a participant tried a status other than Cancelled.</response>
    /// <response code="404">Enrollment not found.</response>
    [HttpPut("enrollments/{id:int}")]
    [SessionAuthorize]
    [ProducesResponseType<EnrollmentResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateEnrollmentStatus(int id, UpdateEnrollmentStatusRequest request)
    {
        var enrollment = await _db.Enrollments.Include(e => e.Event).SingleOrDefaultAsync(e => e.EnrollmentId == id);
        if (enrollment is null)
            return NotFoundProblem("Enrollment not found.");

        var status = request.Status!.Value;
        if (IsEventOrganiser(enrollment))
        {
            enrollment.Status = status;
        }
        else if (IsEntrant(enrollment))
        {
            if (status != EnrollmentStatus.Cancelled)
                return ForbiddenProblem("Participants can only cancel their own entries.");
            enrollment.Status = status;
        }
        else
        {
            return ForbiddenProblem("You can only change your own enrollments or those for your events.");
        }

        await _db.SaveChangesAsync();
        return Ok(await Project(id));
    }

    /// <summary>Withdraw from an event by deleting your enrollment (Participant).</summary>
    /// <param name="id">EnrollmentId.</param>
    /// <response code="204">Enrollment removed.</response>
    /// <response code="401">Not logged in.</response>
    /// <response code="403">Not a participant, or not your enrollment.</response>
    /// <response code="404">Enrollment not found.</response>
    /// <response code="409">A result has already been captured for this entry.</response>
    [HttpDelete("enrollments/{id:int}")]
    [SessionAuthorize(UserRole.Participant)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> DeleteEnrollment(int id)
    {
        var enrollment = await _db.Enrollments.FindAsync(id);
        if (enrollment is null)
            return NotFoundProblem("Enrollment not found.");
        if (!IsEntrant(enrollment))
            return ForbiddenProblem("You can only withdraw your own enrollments.");

        var hasResult = await _db.Results.AnyAsync(r =>
            r.ParticipantId == enrollment.ParticipantId && r.EventId == enrollment.EventId);
        if (hasResult)
            return ConflictProblem("A result has already been recorded for this entry, so it cannot be removed.");

        _db.Enrollments.Remove(enrollment);
        await _db.SaveChangesAsync();

        return NoContent();
    }

    private bool IsEntrant(Enrollment e) =>
        HttpContext.Session.GetRole() == UserRole.Participant && e.ParticipantId == CurrentProfileId;

    private bool IsEventOrganiser(Enrollment e) =>
        HttpContext.Session.GetRole() == UserRole.Organiser && e.Event.OrganiserId == CurrentProfileId;

    private Task<EnrollmentResponse> Project(int enrollmentId) => _db.Enrollments.AsNoTracking()
        .Where(e => e.EnrollmentId == enrollmentId)
        .Select(EnrollmentResponse.Projection)
        .SingleAsync();
}
