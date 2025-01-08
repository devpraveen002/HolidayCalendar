using HolidayCalendar.src.HolidayCalendar.Core.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace HolidayCalendar.src.HolidayCalendar.Infrastructure.Data
{
    public static class DbInitializer
    {
        public static async Task Initialize(ApplicationDbContext context, IServiceProvider services)
        {
            try
            {
                // Get required services
                var userManager = services.GetRequiredService<UserManager<User>>();
                var roleManager = services.GetRequiredService<RoleManager<IdentityRole<long>>>();

                // Ensure database is created and migrations are applied
                await context.Database.MigrateAsync();

                // Create roles if they don't exist
                string[] roles = { "Admin", "User" };
                foreach (var role in roles)
                {
                    if (!await roleManager.RoleExistsAsync(role))
                    {
                        await roleManager.CreateAsync(new IdentityRole<long>(role));
                    }
                }

                // Create admin user if it doesn't exist
                var adminEmail = "admin@example.com";
                if (await userManager.FindByEmailAsync(adminEmail) == null)
                {
                    var adminUser = new User
                    {
                        UserName = adminEmail,
                        Email = adminEmail,
                        EmailConfirmed = true,
                        CreatedAt = DateTime.UtcNow,
                        CreatedBy = 1
                    };

                    var result = await userManager.CreateAsync(adminUser, "Admin@123");
                    if (result.Succeeded)
                    {
                        await userManager.AddToRoleAsync(adminUser, "Admin");
                    }
                }

                // Check if we already have a default calendar
                if (!await context.Calendars.AnyAsync(c => c.IsDefault))
                {
                    // Create default calendar
                    var defaultCalendar = new Calendar
                    {
                        Id = Guid.NewGuid(),
                        Name = "Default Holiday Calendar",
                        IsDefault = true,
                        ShareableLink = Guid.NewGuid().ToString(),
                        CreatedAt = DateTime.UtcNow,
                        CreatedBy = 1 // System user ID
                    };

                    // Create default holidays
                    var defaultHolidays = new List<Holiday>
                    {
                        new Holiday
                        {
                            Id = Guid.NewGuid(),
                            Name = "New Year's Day",
                            Date = new DateTime(DateTime.UtcNow.Year, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                            IsFixedHoliday = true,
                            IsWeekendAdjustable = true,
                            IsDefault = true,
                            Description = "New Year's Day celebration",
                            CreatedAt = DateTime.UtcNow,
                            CreatedBy = 1
                        },
                        new Holiday
                        {
                            Id = Guid.NewGuid(),
                            Name = "Christmas Day",
                            Date = new DateTime(DateTime.UtcNow.Year, 12, 25, 0, 0, 0, DateTimeKind.Utc),
                            IsFixedHoliday = true,
                            IsWeekendAdjustable = true,
                            IsDefault = true,
                            Description = "Christmas Day celebration",
                            CreatedAt = DateTime.UtcNow,
                            CreatedBy = 1
                        }
                    };

                    // Create CalendarHoliday associations
                    var calendarHolidays = defaultHolidays.Select(holiday => new CalendarHoliday
                    {
                        Id = Guid.NewGuid(),
                        CalendarId = defaultCalendar.Id,
                        HolidayId = holiday.Id,
                        CreatedAt = DateTime.UtcNow,
                        CreatedBy = 1
                    }).ToList();

                    // Begin transaction
                    using var transaction = await context.Database.BeginTransactionAsync();
                    try
                    {
                        // Add all entities
                        await context.Calendars.AddAsync(defaultCalendar);
                        await context.Holidays.AddRangeAsync(defaultHolidays);
                        await context.CalendarHolidays.AddRangeAsync(calendarHolidays);

                        // Save changes
                        await context.SaveChangesAsync();

                        // Commit transaction
                        await transaction.CommitAsync();
                    }
                    catch (Exception)
                    {
                        // Rollback on error
                        await transaction.RollbackAsync();
                        throw;
                    }
                }
            }
            catch (Exception)
            {
                throw;
            }
        }

        public static async Task CreateUserDefaultCalendar(ApplicationDbContext context, long userId)
        {
            // Get default calendar
            var defaultCalendar = await context.Calendars
                .FirstOrDefaultAsync(c => c.IsDefault);

            if (defaultCalendar != null)
            {
                // Get default holidays through CalendarHoliday junction
                var defaultHolidayIds = await context.CalendarHolidays
                    .Where(ch => ch.CalendarId == defaultCalendar.Id)
                    .Select(ch => ch.HolidayId)
                    .ToListAsync();

                var defaultHolidays = await context.Holidays
                    .Where(h => defaultHolidayIds.Contains(h.Id))
                    .ToListAsync();

                // Create new calendar for user
                var userCalendar = new Calendar
                {
                    Id = Guid.NewGuid(),
                    Name = "My Calendar",
                    IsDefault = false,
                    ShareableLink = Guid.NewGuid().ToString(),
                    CreatedAt = DateTime.UtcNow,
                    CreatedBy = userId
                };

                // Create UserCalendar association
                var userCalendarAssociation = new UserCalendar
                {
                    Id = Guid.NewGuid(),
                    UserId = userId,
                    CalendarId = userCalendar.Id,
                    CreatedAt = DateTime.UtcNow,
                    CreatedBy = userId
                };

                // Create CalendarHoliday associations for user's calendar
                var calendarHolidays = defaultHolidays.Select(holiday => new CalendarHoliday
                {
                    Id = Guid.NewGuid(),
                    CalendarId = userCalendar.Id,
                    HolidayId = holiday.Id,
                    CreatedAt = DateTime.UtcNow,
                    CreatedBy = userId
                }).ToList();

                // Begin transaction
                using var transaction = await context.Database.BeginTransactionAsync();
                try
                {
                    // Add all entities
                    await context.Calendars.AddAsync(userCalendar);
                    await context.UserCalendars.AddAsync(userCalendarAssociation);
                    await context.CalendarHolidays.AddRangeAsync(calendarHolidays);

                    // Save changes
                    await context.SaveChangesAsync();

                    // Commit transaction
                    await transaction.CommitAsync();
                }
                catch (Exception)
                {
                    // Rollback on error
                    await transaction.RollbackAsync();
                    throw;
                }
            }
        }
    }
}