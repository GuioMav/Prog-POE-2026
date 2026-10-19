using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.API.Auth;
using RaceDay.API.Data;
using RaceDay.API.Dtos;
using RaceDay.API.Models;

namespace RaceDay.API.Controllers;

/// <summary>Registration, login and logout. Login starts a server-side session.</summary>
[Route("api/auth")]
public class AuthController : RaceDayControllerBase
{
    private const int FirstOrganiserNumber = 1001;

    private readonly RaceDayDbContext _db;
    private readonly IPasswordHasher<User> _hasher;

    public AuthController(RaceDayDbContext db, IPasswordHasher<User> hasher)
    {
        _db = db;
        _hasher = hasher;
    }

    /// <summary>Register a new Organiser or Participant account.</summary>
    /// <remarks>
    /// Creates the Users row plus the matching Organiser or Participant row in one save.
    /// The password is hashed before storage. Organisers are given the next organiser number.
    /// </remarks>
    /// <response code="201">Account created; returns the new profile.</response>
    /// <response code="400">Validation failed (missing fields, weak password, invalid role data).</response>
    /// <response code="409">Email or username already in use.</response>
    [HttpPost("register")]
    [ProducesResponseType<UserProfileResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Register(RegisterRequest request)
    {
        var email = request.Email.Trim().ToLowerInvariant();
        var userName = request.UserName.Trim();

        if (await _db.Users.AnyAsync(u => u.Email == email))
            return ConflictProblem("That email address is already registered.");
        if (await _db.Users.AnyAsync(u => u.UserName == userName))
            return ConflictProblem("That username is already taken.");

        var role = request.Role!.Value;
        var user = new User
        {
            Email = email,
            UserName = userName,
            Role = role,
            AccountType = role == UserRole.Organiser
                ? AccountType.Admin
                : request.AccountType ?? AccountType.Standard,
            CreatedAt = DateTime.UtcNow
        };
        user.PasswordHash = _hasher.HashPassword(user, request.Password);

        if (role == UserRole.Organiser)
        {
            var lastNumber = await _db.Organisers.MaxAsync(o => (int?)o.OrganiserNumber) ?? FirstOrganiserNumber - 1;
            user.Organiser = new Organiser
            {
                OrganisationName = request.Name.Trim(),
                OrganiserNumber = lastNumber + 1
            };
        }
        else
        {
            user.Participant = new Participant
            {
                ParticipantName = request.Name.Trim(),
                DateOfBirth = request.DateOfBirth
            };
        }

        _db.Users.Add(user);
        await _db.SaveChangesAsync();

        return Created($"/api/users/{user.UsersId}", UserProfileResponse.From(user));
    }

    /// <summary>Log in with email and password.</summary>
    /// <remarks>
    /// On success the user's id, role and profile id are stored in the server-side session and a
    /// session cookie is returned. Swagger UI sends this cookie automatically on later requests.
    /// </remarks>
    /// <response code="200">Logged in; returns the user's profile.</response>
    /// <response code="400">Email or password missing.</response>
    /// <response code="401">Email or password is incorrect.</response>
    [HttpPost("login")]
    [ProducesResponseType<UserProfileResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Login(LoginRequest request)
    {
        var email = request.Email.Trim().ToLowerInvariant();
        var user = await _db.Users
            .Include(u => u.Organiser)
            .Include(u => u.Participant)
            .SingleOrDefaultAsync(u => u.Email == email);

        // Same message for unknown email and wrong password so accounts can't be probed
        if (user is null || _hasher.VerifyHashedPassword(user, user.PasswordHash, request.Password) == PasswordVerificationResult.Failed)
            return UnauthorizedProblem("Invalid email or password.");

        var profile = UserProfileResponse.From(user);
        HttpContext.Session.SignIn(user, profile.ProfileId);

        return Ok(profile);
    }

    /// <summary>Log out and clear the server-side session.</summary>
    /// <response code="204">Session cleared.</response>
    [HttpPost("logout")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public IActionResult Logout()
    {
        HttpContext.Session.Clear();
        return NoContent();
    }

    /// <summary>Get the profile of the user in the current session.</summary>
    /// <remarks>Lets a client (the Part 3 MVC app, or Swagger) check who is logged in.</remarks>
    /// <response code="200">The logged-in user's profile.</response>
    /// <response code="401">Not logged in.</response>
    [HttpGet("me")]
    [SessionAuthorize]
    [ProducesResponseType<UserProfileResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Me()
    {
        var userId = CurrentUserId;
        var user = await _db.Users
            .Include(u => u.Organiser)
            .Include(u => u.Participant)
            .SingleOrDefaultAsync(u => u.UsersId == userId);

        if (user is null)
        {
            // The account was removed after login - the session is no longer valid
            HttpContext.Session.Clear();
            return UnauthorizedProblem("Your session is no longer valid.");
        }

        return Ok(UserProfileResponse.From(user));
    }
}
