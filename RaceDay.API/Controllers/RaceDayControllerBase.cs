using Microsoft.AspNetCore.Mvc;
using RaceDay.API.Auth;

namespace RaceDay.API.Controllers;

/// <summary>
/// Shared helpers for RaceDay controllers: the current session's ids and
/// consistent ProblemDetails error responses.
/// </summary>
[ApiController]
[Produces("application/json")]
public abstract class RaceDayControllerBase : ControllerBase
{
    /// <summary>UsersId of the logged-in user (null when not logged in).</summary>
    protected int? CurrentUserId => HttpContext.Session.GetUserId();

    /// <summary>OrganiserId or ParticipantId of the logged-in user.</summary>
    protected int? CurrentProfileId => HttpContext.Session.GetProfileId();

    protected ObjectResult NotFoundProblem(string detail) =>
        Problem(statusCode: StatusCodes.Status404NotFound, title: "Not Found", detail: detail);

    protected ObjectResult ForbiddenProblem(string detail) =>
        Problem(statusCode: StatusCodes.Status403Forbidden, title: "Forbidden", detail: detail);

    protected ObjectResult ConflictProblem(string detail) =>
        Problem(statusCode: StatusCodes.Status409Conflict, title: "Conflict", detail: detail);

    protected ObjectResult UnauthorizedProblem(string detail) =>
        Problem(statusCode: StatusCodes.Status401Unauthorized, title: "Unauthorized", detail: detail);
}
