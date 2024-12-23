using HolidayCalendar.src.HolidayCalendar.Core.Entities;
using Microsoft.EntityFrameworkCore;

namespace HolidayCalendar.src.HolidayCalendar.Infrastructure.Data;

public static class DbInitializer  // Added class declaration
{
    public static async Task Initialize(ApplicationDbContext context)  // Changed method name to Initialize
    {
        try
        {
            // Ensure database is created
            await context.Database.MigrateAsync();

            // Check if we already have a default calendar
            if (!await context.Calendars.AnyAsync(c => c.IsDefault))
            {
                var defaultCalendar = new Calendar
                {
                    Name = "Default Holiday Calendar",
                    IsDefault = true,
                    ShareableLink = Guid.NewGuid().ToString(),
                    UserId = null,  // Explicitly set to null for default calendar
                    Holidays = new List<Holiday>
                        {
                            new Holiday
                            {
                                Name = "New Year's Day",
                                Date = new DateTime(DateTime.UtcNow.Year, 1, 1, 0, 0, 0, DateTimeKind.Utc), // Set UTC time
                                IsFixedHoliday = true,
                                IsWeekendAdjustable = true
                            },
                            new Holiday
                            {
                                Name = "Christmas Day",
                                Date = new DateTime(DateTime.UtcNow.Year, 12, 25, 0, 0, 0, DateTimeKind.Utc), // Set UTC time
                                IsFixedHoliday = true,
                                IsWeekendAdjustable = true
                            }
                        }
                };

                context.Calendars.Add(defaultCalendar);
                await context.SaveChangesAsync();
            }
        }
        catch (Exception ex)
        {
            throw;
        }
    }
}
