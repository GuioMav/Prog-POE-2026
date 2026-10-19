using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using RaceDay.API.Auth;
using RaceDay.API.Models;

namespace RaceDay.API.Tests.Unit;

/// <summary>Unit tests for the session-based authorization filter, without running the web server.</summary>
public class SessionAuthorizeAttributeTests
{
    private static AuthorizationFilterContext ContextWith(User? loggedIn)
    {
        var services = new ServiceCollection().AddLogging().AddMvc().Services.BuildServiceProvider();
        var http = new DefaultHttpContext { RequestServices = services, Session = new FakeSession() };
        if (loggedIn is not null)
            http.Session.SignIn(loggedIn, profileId: 42);

        var actionContext = new ActionContext(http, new RouteData(), new ActionDescriptor());
        return new AuthorizationFilterContext(actionContext, []);
    }

    private static User UserIn(UserRole role) => new() { UsersId = 7, Role = role };

    [Fact]
    public void NoSession_Returns401()
    {
        var context = ContextWith(loggedIn: null);

        new SessionAuthorizeAttribute(UserRole.Organiser).OnAuthorization(context);

        var result = Assert.IsType<ObjectResult>(context.Result);
        Assert.Equal(401, result.StatusCode);
    }

    [Fact]
    public void WrongRole_Returns403()
    {
        var context = ContextWith(UserIn(UserRole.Participant));

        new SessionAuthorizeAttribute(UserRole.Organiser).OnAuthorization(context);

        var result = Assert.IsType<ObjectResult>(context.Result);
        Assert.Equal(403, result.StatusCode);
    }

    [Theory]
    [InlineData(UserRole.Organiser)]
    [InlineData(UserRole.Participant)]
    public void MatchingRole_IsAllowed(UserRole role)
    {
        var context = ContextWith(UserIn(role));

        new SessionAuthorizeAttribute(role).OnAuthorization(context);

        Assert.Null(context.Result);
    }

    [Fact]
    public void NoRolesRequired_AllowsAnyLoggedInUser()
    {
        var context = ContextWith(UserIn(UserRole.Participant));

        new SessionAuthorizeAttribute().OnAuthorization(context);

        Assert.Null(context.Result);
    }

    [Fact]
    public void SignIn_StoresUserIdRoleAndProfileIdInSession()
    {
        var session = new FakeSession();

        session.SignIn(UserIn(UserRole.Organiser), profileId: 3);

        Assert.Equal(7, session.GetUserId());
        Assert.Equal(UserRole.Organiser, session.GetRole());
        Assert.Equal(3, session.GetProfileId());
    }

    /// <summary>Minimal in-memory ISession for unit tests.</summary>
    private sealed class FakeSession : ISession
    {
        private readonly Dictionary<string, byte[]> _store = new();

        public bool IsAvailable => true;
        public string Id { get; } = Guid.NewGuid().ToString();
        public IEnumerable<string> Keys => _store.Keys;
        public void Clear() => _store.Clear();
        public Task CommitAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task LoadAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
        public void Remove(string key) => _store.Remove(key);
        public void Set(string key, byte[] value) => _store[key] = value;
        public bool TryGetValue(string key, [NotNullWhen(true)] out byte[]? value) => _store.TryGetValue(key, out value);
    }
}
