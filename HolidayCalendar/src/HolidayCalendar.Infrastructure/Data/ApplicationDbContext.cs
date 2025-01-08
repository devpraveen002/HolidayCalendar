using HolidayCalendar.src.HolidayCalendar.Core.Entities;
using HolidayCalendar.src.HolidayCalendar.Infrastructure.Data.Configrations;
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

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        
        //builder.ApplyConfiguration(new UserConfiguration());
        //builder.ApplyConfiguration(new CalendarConfiguration());
        //builder.ApplyConfiguration(new EventConfiguration());
        

        // Configure Identity tables
        builder.Entity<User>().ToTable("Users");
        builder.Entity<IdentityRole<long>>().ToTable("Roles");
        builder.Entity<IdentityUserRole<long>>().ToTable("UserRoles");
        builder.Entity<IdentityUserClaim<long>>().ToTable("UserClaims");
        builder.Entity<IdentityUserLogin<long>>().ToTable("UserLogins");
        builder.Entity<IdentityRoleClaim<long>>().ToTable("RoleClaims");
        builder.Entity<IdentityUserToken<long>>().ToTable("UserTokens");

        // Configure Calendar
        builder.Entity<Calendar>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedOnAdd();
            entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
            entity.Property(e => e.ShareableLink).HasMaxLength(100);

            // Audit properties configuration
            entity.Property(e => e.CreatedAt).IsRequired();
            entity.Property(e => e.CreatedBy).IsRequired();
        });

        // Configure Holiday
        builder.Entity<Holiday>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedOnAdd();
            entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Description).HasMaxLength(500);
            entity.Property(e => e.Date).IsRequired();

            // Audit properties configuration
            entity.Property(e => e.CreatedAt).IsRequired();
            entity.Property(e => e.CreatedBy).IsRequired();
        });

        // Configure UserCalendar
        builder.Entity<UserCalendar>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedOnAdd();

            // Relationships
            entity.HasOne(uc => uc.User)
                .WithMany()
                .HasForeignKey(uc => uc.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(uc => uc.Calendar)
                .WithMany()
                .HasForeignKey(uc => uc.CalendarId)
                .OnDelete(DeleteBehavior.Restrict);

            // Create unique index on UserId and CalendarId
            entity.HasIndex(uc => new { uc.UserId, uc.CalendarId }).IsUnique();

            // Audit properties configuration
            entity.Property(e => e.CreatedAt).IsRequired();
            entity.Property(e => e.CreatedBy).IsRequired();
        });

        // Configure CalendarHoliday
        builder.Entity<CalendarHoliday>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedOnAdd();

            // Relationships
            entity.HasOne(ch => ch.Calendar)
                .WithMany()
                .HasForeignKey(ch => ch.CalendarId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(ch => ch.Holiday)
                .WithMany()
                .HasForeignKey(ch => ch.HolidayId)
                .OnDelete(DeleteBehavior.Restrict);

            // Create unique index on CalendarId and HolidayId
            entity.HasIndex(ch => new { ch.CalendarId, ch.HolidayId }).IsUnique();

            // Audit properties configuration
            entity.Property(e => e.CreatedAt).IsRequired();
            entity.Property(e => e.CreatedBy).IsRequired();
        });

        // Configure Event
        builder.Entity<Event>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedOnAdd();
            entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Description).HasMaxLength(500);
            entity.Property(e => e.StartDate).IsRequired();
            entity.Property(e => e.EndDate).IsRequired();

            // Relationships
            entity.HasOne(e => e.Calendar)
                .WithMany()
                .HasForeignKey(e => e.CalendarId)
                .OnDelete(DeleteBehavior.Restrict);

            // Audit properties configuration
            entity.Property(e => e.CreatedAt).IsRequired();
            entity.Property(e => e.CreatedBy).IsRequired();

            // Add check constraint for EndDate >= StartDate
            //entity.HasCheckConstraint("CK_Event_DateRange", "EndDate >= StartDate");
        });

        // Add global query filters if needed
        // builder.Entity<Calendar>().HasQueryFilter(e => !e.IsDeleted);  // If you add soft delete
    }
}