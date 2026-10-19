using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.API.Auth;
using RaceDay.API.Data;
using RaceDay.API.Dtos;
using RaceDay.API.Models;

namespace RaceDay.API.Controllers;

/// <summary>User profiles. Both roles can view and update only their own profile.</summary>
[ApiController]
[Route("api/users")]
[Produces("application/json")]
[SessionAuthorize]
[ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
public class UsersController : ControllerBase
{
    private readonly RaceDayDbContext _db;

    public UsersController(RaceDayDbContext db)
    {
        _db = db;
    }

    /// <summary>Get a user profile (your own only).</summary>
    /// <param name="id">UsersId of the logged-in user.</param>
    /// <response code="200">The profile.</response>
    /// <response code="401">Not logged in.</response>
    /// <response code="403">The id belongs to a different user.</response>
    /// <response code="404">User not found.</response>
    [HttpGet("{id:int}")]
    [ProducesResponseType<UserProfileResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetUser(int id)
    {
        if (id != HttpContext.Session.GetUserId())
            return Problem(statusCode: 403, title: "Forbidden", detail: "You can only view your own profile.");

        var user = await LoadUser(id);
        if (user is null)
            return Problem(statusCode: 404, title: "Not Found", detail: "User not found.");

        return Ok(UserProfileResponse.From(user));
    }

    /// <summary>Update your profile details.</summary>
    /// <remarks>Email, username and name can be changed. Date of birth applies to participants only.</remarks>
    /// <param name="id">UsersId of the logged-in user.</param>
    /// <param name="request">New profile values.</param>
    /// <response code="200">Updated profile.</response>
    /// <response code="400">Validation failed.</response>
    /// <response code="401">Not logged in.</response>
    /// <response code="403">The id belongs to a different user.</response>
    /// <response code="404">User not found.</response>
    /// <response code="409">Email or username already used by another account.</response>
    [HttpPut("{id:int}")]
    [ProducesResponseType<UserProfileResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> UpdateUser(int id, UpdateProfileRequest request)
    {
        if (id != HttpContext.Session.GetUserId())
            return Problem(statusCode: 403, title: "Forbidden", detail: "You can only update your own profile.");

        var user = await LoadUser(id);
        if (user is null)
            return Problem(statusCode: 404, title: "Not Found", detail: "User not found.");

        var email = request.Email.Trim().ToLowerInvariant();
        var userName = request.UserName.Trim();

        if (await _db.Users.AnyAsync(u => u.UsersId != id && u.Email == email))
            return Problem(statusCode: 409, title: "Conflict", detail: "That email address is already registered.");
        if (await _db.Users.AnyAsync(u => u.UsersId != id && u.UserName == userName))
            return Problem(statusCode: 409, title: "Conflict", detail: "That username is already taken.");

        if (request.DateOfBirth is { } dob && dob > DateOnly.FromDateTime(DateTime.UtcNow))
        {
            ModelState.AddModelError(nameof(request.DateOfBirth), "Date of birth cannot be in the future.");
            return ValidationProblem(ModelState);
        }

        user.Email = email;
        user.UserName = userName;

        if (user.Role == UserRole.Organiser && user.Organiser is not null)
        {
            user.Organiser.OrganisationName = request.Name.Trim();
        }
        else if (user.Participant is not null)
        {
            user.Participant.ParticipantName = request.Name.Trim();
            user.Participant.DateOfBirth = request.DateOfBirth;
        }

        await _db.SaveChangesAsync();
        return Ok(UserProfileResponse.From(user));
    }

    private Task<User?> LoadUser(int id) => _db.Users
        .Include(u => u.Organiser)
        .Include(u => u.Participant)
        .SingleOrDefaultAsync(u => u.UsersId == id);
}
