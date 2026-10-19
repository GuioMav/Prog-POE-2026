using System.Reflection;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi;
using RaceDay.API.Data;
using RaceDay.API.Models;

var builder = WebApplication.CreateBuilder(args);

// Database: SQL Server through EF Core (Code-First). The connection string can be
// overridden with the ConnectionStrings__DefaultConnection environment variable.
builder.Services.AddDbContext<RaceDayDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Server-side session: the session data (user id + role) lives in server memory,
// the client only receives an HttpOnly cookie holding the session id.
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.Cookie.Name = ".RaceDay.Session";
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    options.Cookie.SameSite = SameSiteMode.Lax;
    options.IdleTimeout = TimeSpan.FromMinutes(30);
});

// ASP.NET Core's built-in PBKDF2 password hasher (salted, iterated)
builder.Services.AddSingleton<IPasswordHasher<User>, PasswordHasher<User>>();

// Errors are returned as RFC 7807 ProblemDetails JSON
builder.Services.AddProblemDetails();

builder.Services.AddControllers()
    // Send/receive enums as text ("Run", "Organiser") instead of numbers
    .AddJsonOptions(o => o.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "RaceDay API",
        Version = "v1",
        Description =
            "REST API for RaceDay, the South African road running, walking and cycling event platform.\n\n" +
            "**How to test protected endpoints:** call `POST /api/auth/login` first. The API stores your user id " +
            "and role in a server-side session and the browser keeps the session cookie, so every later request " +
            "from this page is made as that user. Call `POST /api/auth/logout` to end the session.\n\n" +
            "Seeded accounts (password `Password123!`): organiser `david@example.com`, participant `alice@example.com`."
    });

    // Pull the /// summaries, remarks and response descriptions from the XML documentation file
    var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    options.IncludeXmlComments(Path.Combine(AppContext.BaseDirectory, xmlFile));
});

var app = builder.Build();

// Create/upgrade the database from the EF migrations and add sample data when empty.
// Tests switch this off (Database:MigrateAndSeed = false) and use an in-memory database.
if (app.Configuration.GetValue("Database:MigrateAndSeed", true))
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<RaceDayDbContext>();
    await db.Database.MigrateAsync();
    await DbSeeder.SeedAsync(db, scope.ServiceProvider.GetRequiredService<IPasswordHasher<User>>());
}

// Unhandled exceptions become a generic 500 ProblemDetails (no stack trace sent to the client)
app.UseExceptionHandler();
app.UseStatusCodePages();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.DocumentTitle = "RaceDay API";
        options.DisplayRequestDuration();
    });
}

app.UseHttpsRedirection();
app.UseSession();
app.MapControllers();

app.Run();

// Exposes the implicit Program class so the test project can host the API in memory
public partial class Program { }
