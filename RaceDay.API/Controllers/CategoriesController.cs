using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.API.Auth;
using RaceDay.API.Data;
using RaceDay.API.Dtos;
using RaceDay.API.Models;

namespace RaceDay.API.Controllers;

/// <summary>
/// Age or distance categories within an event. Anyone can view them; only the
/// organiser who owns the event can add, change or remove them.
/// </summary>
[Route("api")]
public class CategoriesController : RaceDayControllerBase
{
    private readonly RaceDayDbContext _db;

    public CategoriesController(RaceDayDbContext db)
    {
        _db = db;
    }

    /// <summary>List the categories available for an event.</summary>
    /// <param name="eventId">EventId.</param>
    /// <response code="200">The event's categories.</response>
    /// <response code="404">Event not found.</response>
    [HttpGet("events/{eventId:int}/categories")]
    [ProducesResponseType<List<CategoryResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetCategories(int eventId)
    {
        if (!await _db.Events.AnyAsync(e => e.EventId == eventId))
            return NotFoundProblem("Event not found.");

        var categories = await _db.Categories.AsNoTracking()
            .Where(c => c.EventId == eventId)
            .OrderBy(c => c.CategoryName)
            .ToListAsync();

        return Ok(categories.Select(CategoryResponse.From));
    }

    /// <summary>Add a category to one of your events (Organiser).</summary>
    /// <param name="eventId">EventId.</param>
    /// <param name="request">Category name and type (Age or Distance).</param>
    /// <response code="201">Category created.</response>
    /// <response code="400">Validation failed.</response>
    /// <response code="401">Not logged in.</response>
    /// <response code="403">Not an organiser, or the event belongs to another organiser.</response>
    /// <response code="404">Event not found.</response>
    /// <response code="409">The event already has a category with that name.</response>
    [HttpPost("events/{eventId:int}/categories")]
    [SessionAuthorize(UserRole.Organiser)]
    [ProducesResponseType<CategoryResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> CreateCategory(int eventId, CategoryRequest request)
    {
        var raceEvent = await _db.Events.FindAsync(eventId);
        if (raceEvent is null)
            return NotFoundProblem("Event not found.");
        if (raceEvent.OrganiserId != CurrentProfileId)
            return ForbiddenProblem("You can only add categories to events you created.");

        var name = request.CategoryName.Trim();
        if (await NameTaken(eventId, name, exceptCategoryId: null))
            return ConflictProblem($"This event already has a category called '{name}'.");

        var category = new Category
        {
            EventId = eventId,
            CategoryName = name,
            CategoryType = request.CategoryType!.Value
        };

        _db.Categories.Add(category);
        await _db.SaveChangesAsync();

        return Created($"/api/events/{eventId}/categories", CategoryResponse.From(category));
    }

    /// <summary>Update a category on one of your events (Organiser).</summary>
    /// <param name="id">CategoryId.</param>
    /// <param name="request">New name and type.</param>
    /// <response code="200">Updated category.</response>
    /// <response code="400">Validation failed.</response>
    /// <response code="401">Not logged in.</response>
    /// <response code="403">Not an organiser, or the event belongs to another organiser.</response>
    /// <response code="404">Category not found.</response>
    /// <response code="409">Another category in the event already has that name.</response>
    [HttpPut("categories/{id:int}")]
    [SessionAuthorize(UserRole.Organiser)]
    [ProducesResponseType<CategoryResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> UpdateCategory(int id, CategoryRequest request)
    {
        var category = await _db.Categories.Include(c => c.Event).SingleOrDefaultAsync(c => c.CategoryId == id);
        if (category is null)
            return NotFoundProblem("Category not found.");
        if (category.Event.OrganiserId != CurrentProfileId)
            return ForbiddenProblem("You can only change categories on events you created.");

        var name = request.CategoryName.Trim();
        if (await NameTaken(category.EventId, name, exceptCategoryId: id))
            return ConflictProblem($"This event already has a category called '{name}'.");

        category.CategoryName = name;
        category.CategoryType = request.CategoryType!.Value;
        await _db.SaveChangesAsync();

        return Ok(CategoryResponse.From(category));
    }

    /// <summary>Delete a category from one of your events (Organiser).</summary>
    /// <remarks>Not allowed while participants are entered in the category.</remarks>
    /// <param name="id">CategoryId.</param>
    /// <response code="204">Category deleted.</response>
    /// <response code="401">Not logged in.</response>
    /// <response code="403">Not an organiser, or the event belongs to another organiser.</response>
    /// <response code="404">Category not found.</response>
    /// <response code="409">Participants are enrolled in this category.</response>
    [HttpDelete("categories/{id:int}")]
    [SessionAuthorize(UserRole.Organiser)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> DeleteCategory(int id)
    {
        var category = await _db.Categories.Include(c => c.Event).SingleOrDefaultAsync(c => c.CategoryId == id);
        if (category is null)
            return NotFoundProblem("Category not found.");
        if (category.Event.OrganiserId != CurrentProfileId)
            return ForbiddenProblem("You can only delete categories on events you created.");
        if (await _db.Enrollments.AnyAsync(e => e.CategoryId == id))
            return ConflictProblem("Participants are enrolled in this category, so it cannot be deleted.");

        _db.Categories.Remove(category);
        await _db.SaveChangesAsync();

        return NoContent();
    }

    private Task<bool> NameTaken(int eventId, string name, int? exceptCategoryId) =>
        _db.Categories.AnyAsync(c =>
            c.EventId == eventId &&
            c.CategoryName == name &&
            c.CategoryId != exceptCategoryId);
}
