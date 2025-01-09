using HolidayCalendar.src.HolidayCalendar.Core.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace HolidayCalendar.src.HolidayCalendar.Infrastructure.Data;

public class ApplicationDbContext : IdentityDbContext<User, IdentityRole<long>, long,
    IdentityUserClaim<long>, IdentityUserRole<long>, IdentityUserLogin<long>,
    IdentityRoleClaim<long>, IdentityUserToken<long>>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<Calendar> Calendars { get; set; }
    public DbSet<Holiday> Holidays { get; set; }
    public DbSet<UserCalendar> UserCalendars { get; set; }
    public DbSet<CalendarHoliday> CalendarHolidays { get; set; }
    public DbSet<Event> Events { get; set; }
    public DbSet<Country> Countries { get; set; }
    public DbSet<ExportLog> ExportLogs { get; set; }
    public DbSet<CalendarShare> CalendarShares { get; set; }
    public DbSet<EventType> EventTypes { get; set; }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<User>().ToTable("Users");
        builder.Entity<IdentityRole<long>>().ToTable("Roles");
        builder.Entity<IdentityUserRole<long>>().ToTable("UserRoles");
        builder.Entity<IdentityUserClaim<long>>().ToTable("UserClaims");
        builder.Entity<IdentityUserLogin<long>>().ToTable("UserLogins");
        builder.Entity<IdentityRoleClaim<long>>().ToTable("RoleClaims");
        builder.Entity<IdentityUserToken<long>>().ToTable("UserTokens");

        builder.Entity<Calendar>()
            .Property(c => c.IsDefault)
            .HasColumnName("IsDefault");

        builder.Entity<Calendar>()
            .HasIndex(c => new { c.Name, c.CountryCode })
            .IsUnique()
            .HasFilter("\"IsDefault\" = true");

        builder.Entity<CalendarHoliday>()
            .HasOne(ch => ch.Calendar)
            .WithMany()
            .HasForeignKey(ch => ch.CalendarId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<CalendarHoliday>()
            .HasOne(ch => ch.Holiday)
            .WithMany()
            .HasForeignKey(ch => ch.HolidayId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<UserCalendar>()
            .HasIndex(uc => new { uc.UserId, uc.CalendarId })
            .IsUnique();

        builder.Entity<Event>()
            .HasIndex(e => new { e.CalendarId, e.StartDate, e.EndDate });

        builder.Entity<CalendarShare>()
            .HasIndex(e => e.ShareableLink).IsUnique();

        builder.Entity<Event>()
            .HasOne(e => e.EventType)
            .WithMany()
            .HasForeignKey(e => e.EventTypeId)
            .OnDelete(DeleteBehavior.Restrict);

        foreach (var property in builder.Model.GetEntityTypes()
            .SelectMany(t => t.GetProperties())
            .Where(p => p.ClrType == typeof(DateTime) || p.ClrType == typeof(DateTime?)))
        {
            property.SetColumnType("timestamp with time zone");
        }
    }

    public override int SaveChanges()
    {
        HandleDateTimeConversion();
        return base.SaveChanges();
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        HandleDateTimeConversion();
        return base.SaveChangesAsync(cancellationToken);
    }

    private void HandleDateTimeConversion()
    {
        foreach (var entry in ChangeTracker.Entries()
            .Where(e => e.State == EntityState.Added || e.State == EntityState.Modified))
        {
            foreach (var property in entry.Properties
                .Where(p => p.Metadata.ClrType == typeof(DateTime) || p.Metadata.ClrType == typeof(DateTime?)))
            {
                if (property.CurrentValue is DateTime currentDateTime)
                {
                    property.CurrentValue = DateTime.SpecifyKind(currentDateTime, DateTimeKind.Utc);
                }
            }
        }
    }
}
