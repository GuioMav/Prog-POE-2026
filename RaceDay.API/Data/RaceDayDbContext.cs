using Microsoft.EntityFrameworkCore;
using RaceDay.API.Models;

namespace RaceDay.API.Data;

/// <summary>
/// EF Core Code-First context. Table names, keys and constraints mirror the
/// Part 1 SQL script (docs/raceday.sql) so both describe the same schema.
/// </summary>
public class RaceDayDbContext : DbContext
{
    public RaceDayDbContext(DbContextOptions<RaceDayDbContext> options) : base(options)
    {
    }

    public DbSet<User> Users => Set<User>();
    public DbSet<Organiser> Organisers => Set<Organiser>();
    public DbSet<Participant> Participants => Set<Participant>();
    public DbSet<Event> Events => Set<Event>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Enrollment> Enrollments => Set<Enrollment>();
    public DbSet<Result> Results => Set<Result>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<User>(e =>
        {
            e.ToTable("Users");
            e.HasKey(u => u.UsersId);
            e.Property(u => u.Email).HasMaxLength(100).IsRequired();
            e.Property(u => u.UserName).HasMaxLength(50).IsRequired();
            e.Property(u => u.PasswordHash).HasMaxLength(255).IsRequired();
            e.Property(u => u.Role).HasConversion<string>().HasMaxLength(20);
            e.Property(u => u.AccountType).HasConversion<string>().HasMaxLength(20);
            e.Property(u => u.CreatedAt).HasDefaultValueSql("GETUTCDATE()");
            e.HasIndex(u => u.Email).IsUnique();
            e.HasIndex(u => u.UserName).IsUnique();
        });

        modelBuilder.Entity<Organiser>(e =>
        {
            e.ToTable("Organiser");
            e.HasKey(o => o.OrganiserId);
            e.Property(o => o.OrganisationName).HasMaxLength(100).IsRequired();
            e.HasIndex(o => o.OrganiserNumber).IsUnique();
            e.HasIndex(o => o.UsersId).IsUnique();
            e.HasOne(o => o.User)
                .WithOne(u => u.Organiser)
                .HasForeignKey<Organiser>(o => o.UsersId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Participant>(e =>
        {
            e.ToTable("Participant");
            e.HasKey(p => p.ParticipantId);
            e.Property(p => p.ParticipantName).HasMaxLength(100).IsRequired();
            e.HasIndex(p => p.UsersId).IsUnique();
            e.HasOne(p => p.User)
                .WithOne(u => u.Participant)
                .HasForeignKey<Participant>(p => p.UsersId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Event>(e =>
        {
            e.ToTable("Events", t =>
            {
                t.HasCheckConstraint("CK_Events_DistanceKm", "[DistanceKm] > 0");
                t.HasCheckConstraint("CK_Events_EntryFee", "[EntryFee] >= 0");
            });
            e.HasKey(ev => ev.EventId);
            e.Property(ev => ev.EventName).HasMaxLength(100).IsRequired();
            e.Property(ev => ev.Description).HasMaxLength(500);
            e.Property(ev => ev.Location).HasMaxLength(100).IsRequired();
            e.Property(ev => ev.DistanceKm).HasPrecision(6, 2);
            e.Property(ev => ev.EventType).HasConversion<string>().HasMaxLength(10);
            e.Property(ev => ev.EntryFee).HasPrecision(10, 2).HasDefaultValue(0m);
            e.HasOne(ev => ev.Organiser)
                .WithMany(o => o.Events)
                .HasForeignKey(ev => ev.OrganiserId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Category>(e =>
        {
            e.ToTable("Category");
            e.HasKey(c => c.CategoryId);
            e.Property(c => c.CategoryName).HasMaxLength(50).IsRequired();
            e.Property(c => c.CategoryType).HasConversion<string>().HasMaxLength(10);
            // The same category name cannot appear twice in one event
            e.HasIndex(c => new { c.EventId, c.CategoryName }).IsUnique();
            e.HasOne(c => c.Event)
                .WithMany(ev => ev.Categories)
                .HasForeignKey(c => c.EventId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Enrollment and Result use Restrict (NO ACTION) on every FK: SQL Server
        // rejects multiple cascade paths (Event -> Category -> Enrollment and
        // Event -> Enrollment), so the EventsController removes children itself.
        modelBuilder.Entity<Enrollment>(e =>
        {
            e.ToTable("Enrollment", t => t.HasCheckConstraint("CK_Enrollment_Price", "[Price] >= 0"));
            e.HasKey(en => en.EnrollmentId);
            e.Property(en => en.Status).HasConversion<string>().HasMaxLength(20);
            e.Property(en => en.Price).HasPrecision(10, 2);
            e.HasIndex(en => new { en.ParticipantId, en.EventId }).IsUnique();
            e.HasOne(en => en.Participant)
                .WithMany(p => p.Enrollments)
                .HasForeignKey(en => en.ParticipantId)
                .OnDelete(DeleteBehavior.Restrict);
            e.HasOne(en => en.Event)
                .WithMany(ev => ev.Enrollments)
                .HasForeignKey(en => en.EventId)
                .OnDelete(DeleteBehavior.Restrict);
            e.HasOne(en => en.Category)
                .WithMany(c => c.Enrollments)
                .HasForeignKey(en => en.CategoryId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Result>(e =>
        {
            e.ToTable("Result", t => t.HasCheckConstraint("CK_Result_Position", "[Position] > 0"));
            e.HasKey(r => r.ResultId);
            e.HasIndex(r => new { r.ParticipantId, r.EventId }).IsUnique();
            // Two finishers cannot share a position in the same event
            e.HasIndex(r => new { r.EventId, r.Position }).IsUnique();
            e.HasOne(r => r.Participant)
                .WithMany(p => p.Results)
                .HasForeignKey(r => r.ParticipantId)
                .OnDelete(DeleteBehavior.Restrict);
            e.HasOne(r => r.Event)
                .WithMany(ev => ev.Results)
                .HasForeignKey(r => r.EventId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }
}
