using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.API.Auth;
using RaceDay.API.Data;
using RaceDay.API.Dtos;
using RaceDay.API.Models;

namespace RaceDay.API.Controllers;

/// <summary>
/// Run, walk and cycle events. Anyone can browse events; only organisers can
/// create them, and an organiser can only change or delete their own events.
/// </summary>
[Route("api")]
public class EventsController : RaceDayControllerBase
{
    private readonly RaceDayDbContext _db;

    public EventsController(RaceDayDbContext db)
    {
        _db = db;
    }

    /// <summary>List all events, optionally filtered.</summary>
    /// <param name="type">Only events of this type (Run, Walk or Cycle).</param>
    /// <param name="location">Only events whose location contains this text.</param>
    /// <param name="from">Only events on or after this date (yyyy-MM-dd).</param>
    /// <param name="to">Only events on or before this date (yyyy-MM-dd).</param>
    /// <response code="200">Events ordered by date.</response>
    [HttpGet("events")]
    [ProducesResponseType<List<EventResponse>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetEvents(
        [FromQuery] EventType? type,
        [FromQuery] string? location,
        [FromQuery] DateOnly? from,
        [FromQuery] DateOnly? to)
    {
        var query = _db.Events.AsNoTracking().AsQueryable();

        if (type is not null) query = query.Where(e => e.EventType == type);
        if (!string.IsNullOrWhiteSpace(location)) query = query.Where(e => e.Location.Contains(location));
        if (from is not null) query = query.Where(e => e.EventDate >= from);
        if (to is not null) query = query.Where(e => e.EventDate <= to);

        var events = await query
            .OrderBy(e => e.EventDate)
            .Select(EventResponse.Projection)
            .ToListAsync();

        return Ok(events);
    }

    /// <summary>List upcoming events (today or later).</summary>
    /// <response code="200">Upcoming events, soonest first.</response>
    [HttpGet("events/upcoming")]
    [ProducesResponseType<List<EventResponse>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetUpcomingEvents()
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        var events = await _db.Events.AsNoTracking()
            .Where(e => e.EventDate >= today)
            .OrderBy(e => e.EventDate)
            .Select(EventResponse.Projection)
            .ToListAsync();

        return Ok(events);
    }

    /// <summary>Get full details of one event, including its categories.</summary>
    /// <param name="id">EventId.</param>
    /// <response code="200">The event and its categories.</response>
    /// <response code="404">Event not found.</response>
    [HttpGet("events/{id:int}")]
    [ProducesResponseType<EventDetailResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetEvent(int id)
    {
        var summary = await _db.Events.AsNoTracking()
            .Where(e => e.EventId == id)
            .Select(EventResponse.Projection)
            .SingleOrDefaultAsync();

        if (summary is null)
            return NotFoundProblem("Event not found.");

        var categories = await _db.Categories.AsNoTracking()
            .Where(c => c.EventId == id)
            .OrderBy(c => c.CategoryName)
            .ToListAsync();

        return Ok(ToDetail(summary, categories));
    }

    /// <summary>Create a new event (Organiser).</summary>
    /// <remarks>The event is owned by the logged-in organiser. The date cannot be in the past.</remarks>
    /// <response code="201">Event created.</response>
    /// <response code="400">Validation failed.</response>
    /// <response code="401">Not logged in.</response>
    /// <response code="403">Logged in as a participant.</response>
    [HttpPost("events")]
    [SessionAuthorize(UserRole.Organiser)]
    [ProducesResponseType<EventDetailResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> CreateEvent(EventRequest request)
    {
        if (request.EventDate < DateOnly.FromDateTime(DateTime.UtcNow))
        {
            ModelState.AddModelError(nameof(request.EventDate), "The event date cannot be in the past.");
            return ValidationProblem(ModelState);
        }

        var raceEvent = new Event { OrganiserId = CurrentProfileId!.Value };
        Apply(request, raceEvent);

        _db.Events.Add(raceEvent);
        await _db.SaveChangesAsync();

        var created = await _db.Events.AsNoTracking()
            .Where(e => e.EventId == raceEvent.EventId)
            .Select(EventResponse.Projection)
            .SingleAsync();

        return CreatedAtAction(nameof(GetEvent), new { id = raceEvent.EventId }, ToDetail(created, []));
    }

    /// <summary>Update one of your events (Organiser).</summary>
    /// <param name="id">EventId.</param>
    /// <param name="request">New event values.</param>
    /// <response code="200">Updated event.</response>
    /// <response code="400">Validation failed.</response>
    /// <response code="401">Not logged in.</response>
    /// <response code="403">Not an organiser, or the event belongs to another organiser.</response>
    /// <response code="404">Event not found.</response>
    [HttpPut("events/{id:int}")]
    [SessionAuthorize(UserRole.Organiser)]
    [ProducesResponseType<EventDetailResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateEvent(int id, EventRequest request)
    {
        var raceEvent = await _db.Events.FindAsync(id);
        if (raceEvent is null)
            return NotFoundProblem("Event not found.");
        if (raceEvent.OrganiserId != CurrentProfileId)
            return ForbiddenProblem("You can only update events you created.");

        Apply(request, raceEvent);
        await _db.SaveChangesAsync();

        return await GetEvent(id);
    }

    /// <summary>Delete one of your events (Organiser).</summary>
    /// <remarks>Also removes the event's categories, enrollments and results.</remarks>
    /// <param name="id">EventId.</param>
    /// <response code="204">Event deleted.</response>
    /// <response code="401">Not logged in.</response>
    /// <response code="403">Not an organiser, or the event belongs to another organiser.</response>
    /// <response code="404">Event not found.</response>
    [HttpDelete("events/{id:int}")]
    [SessionAuthorize(UserRole.Organiser)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteEvent(int id)
    {
        var raceEvent = await _db.Events
            .Include(e => e.Results)
            .Include(e => e.Enrollments)
            .Include(e => e.Categories)
            .SingleOrDefaultAsync(e => e.EventId == id);

        if (raceEvent is null)
            return NotFoundProblem("Event not found.");
        if (raceEvent.OrganiserId != CurrentProfileId)
            return ForbiddenProblem("You can only delete events you created.");

        // Children first: the enrollment/result FKs are NO ACTION in SQL Server
        _db.Results.RemoveRange(raceEvent.Results);
        _db.Enrollments.RemoveRange(raceEvent.Enrollments);
        _db.Categories.RemoveRange(raceEvent.Categories);
        _db.Events.Remove(raceEvent);
        await _db.SaveChangesAsync();

        return NoContent();
    }

    /// <summary>List the events managed by an organiser (your own only).</summary>
    /// <param name="organiserId">OrganiserId of the logged-in organiser.</param>
    /// <response code="200">The organiser's events with category and enrollment counts.</response>
    /// <response code="401">Not logged in.</response>
    /// <response code="403">Not an organiser, or another organiser's id.</response>
    [HttpGet("organisers/{organiserId:int}/events")]
    [SessionAuthorize(UserRole.Organiser)]
    [ProducesResponseType<List<EventResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetOrganiserEvents(int organiserId)
    {
        if (organiserId != CurrentProfileId)
            return ForbiddenProblem("You can only view your own events.");

        var events = await _db.Events.AsNoTracking()
            .Where(e => e.OrganiserId == organiserId)
            .OrderBy(e => e.EventDate)
            .Select(EventResponse.Projection)
            .ToListAsync();

        return Ok(events);
    }

    private static void Apply(EventRequest request, Event target)
    {
        target.EventName = request.EventName.Trim();
        target.EventDate = request.EventDate!.Value;
        target.Description = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim();
        target.Location = request.Location.Trim();
        target.DistanceKm = request.DistanceKm;
        target.EventType = request.EventType!.Value;
        target.EntryFee = request.EntryFee;
    }

    private static EventDetailResponse ToDetail(EventResponse e, IEnumerable<Category> categories) => new()
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
        OrganiserName = e.OrganiserName,
        CategoryCount = e.CategoryCount,
        EnrollmentCount = e.EnrollmentCount,
        Categories = categories.Select(CategoryResponse.From).ToList()
    };
}
