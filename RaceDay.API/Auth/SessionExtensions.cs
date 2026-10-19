using RaceDay.API.Models;

namespace RaceDay.API.Auth;

/// <summary>
/// Keys and helpers for the server-side session. On login the API stores the
/// user's id, role and role-specific profile id (OrganiserId or ParticipantId);
/// the browser only holds the opaque session cookie.
/// </summary>
public static class SessionExtensions
{
    public const string UserIdKey = "UserId";
    public const string RoleKey = "Role";
    public const string ProfileIdKey = "ProfileId";

    public static void SignIn(this ISession session, User user, int profileId)
    {
        session.SetInt32(UserIdKey, user.UsersId);
        session.SetString(RoleKey, user.Role.ToString());
        session.SetInt32(ProfileIdKey, profileId);
    }

    public static int? GetUserId(this ISession session) => session.GetInt32(UserIdKey);

    public static UserRole? GetRole(this ISession session) =>
        Enum.TryParse<UserRole>(session.GetString(RoleKey), out var role) ? role : null;

    /// <summary>OrganiserId for organisers, ParticipantId for participants.</summary>
    public static int? GetProfileId(this ISession session) => session.GetInt32(ProfileIdKey);
}
