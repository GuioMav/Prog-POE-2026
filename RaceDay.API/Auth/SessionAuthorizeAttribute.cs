using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using RaceDay.API.Models;

namespace RaceDay.API.Auth;

/// <summary>
/// Protects a controller or action using the server-side session.
/// <list type="bullet">
/// <item>No user in the session → 401 Unauthorized.</item>
/// <item>Roles given and the session role is not one of them → 403 Forbidden.</item>
/// </list>
/// Usage: <c>[SessionAuthorize]</c> for any logged-in user,
/// <c>[SessionAuthorize(UserRole.Organiser)]</c> for one role.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public class SessionAuthorizeAttribute : Attribute, IAuthorizationFilter
{
    public UserRole[] Roles { get; }

    public SessionAuthorizeAttribute(params UserRole[] roles)
    {
        Roles = roles;
    }

    public void OnAuthorization(AuthorizationFilterContext context)
    {
        var session = context.HttpContext.Session;

        if (session.GetUserId() is null)
        {
            context.Result = Problem(context.HttpContext, StatusCodes.Status401Unauthorized, "Unauthorized",
                "You must be logged in to access this resource.");
            return;
        }

        if (Roles.Length > 0 && (session.GetRole() is not { } role || !Roles.Contains(role)))
        {
            context.Result = Problem(context.HttpContext, StatusCodes.Status403Forbidden, "Forbidden",
                $"This action is only available to: {string.Join(", ", Roles)}.");
        }
    }

    // Uses the framework's factory so these responses match the API's other errors (type, traceId)
    private static ObjectResult Problem(HttpContext http, int status, string title, string detail)
    {
        var factory = http.RequestServices.GetRequiredService<ProblemDetailsFactory>();
        var problem = factory.CreateProblemDetails(http, status, title, detail: detail);
        return new ObjectResult(problem) { StatusCode = status };
    }
}
