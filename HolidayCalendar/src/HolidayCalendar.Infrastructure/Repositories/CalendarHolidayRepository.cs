using HolidayCalendar.src.HolidayCalendar.Core.Entities;
using HolidayCalendar.src.HolidayCalendar.Core.Interfaces;
using HolidayCalendar.src.HolidayCalendar.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace HolidayCalendar.src.HolidayCalendar.Infrastructure.Repositories
{
    public class CalendarHolidayRepository : ICalendarHolidayRepository
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<CalendarHolidayRepository> _logger;

        public CalendarHolidayRepository(ApplicationDbContext context, ILogger<CalendarHolidayRepository> logger)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<CalendarHoliday> CreateAsync(CalendarHoliday calendarHoliday)
        {
            try
            {
                _context.CalendarHolidays.Add(calendarHoliday);
                await _context.SaveChangesAsync();
                return calendarHoliday;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating calendar-holiday association");
                throw;
            }
        }

        public async Task DeleteAsync(Guid calendarId, Guid holidayId)
        {
            try
            {
                var association = await _context.CalendarHolidays
                    .FirstOrDefaultAsync(ch => ch.CalendarId == calendarId && ch.HolidayId == holidayId);

                if (association != null)
                {
                    _context.CalendarHolidays.Remove(association);
                    await _context.SaveChangesAsync();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting calendar-holiday association");
                throw;
            }
        }

        public async Task<IEnumerable<CalendarHoliday>> GetByCalendarIdAsync(Guid calendarId)
        {
            try
            {
                return await _context.CalendarHolidays
                    .Include(ch => ch.Holiday)
                    .Where(ch => ch.CalendarId == calendarId)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting calendar-holiday associations");
                throw;
            }
        }

        // Additional helper methods
        public async Task<bool> ExistsAsync(Guid calendarId, Guid holidayId)
        {
            return await _context.CalendarHolidays
                .AnyAsync(ch => ch.CalendarId == calendarId && ch.HolidayId == holidayId);
        }

        public async Task<CalendarHoliday> GetByIdsAsync(Guid calendarId, Guid holidayId)
        {
            return await _context.CalendarHolidays
                .Include(ch => ch.Holiday)
                .Include(ch => ch.Calendar)
                .FirstOrDefaultAsync(ch => ch.CalendarId == calendarId && ch.HolidayId == holidayId);
        }

        public async Task<IEnumerable<CalendarHoliday>> GetByHolidayIdAsync(Guid holidayId)
        {
            return await _context.CalendarHolidays
                .Include(ch => ch.Calendar)
                .Where(ch => ch.HolidayId == holidayId)
                .ToListAsync();
        }

        public async Task DeleteByCalendarIdAsync(Guid calendarId)
        {
            try
            {
                var associations = await _context.CalendarHolidays
                    .Where(ch => ch.CalendarId == calendarId)
                    .ToListAsync();

                if (associations.Any())
                {
                    _context.CalendarHolidays.RemoveRange(associations);
                    await _context.SaveChangesAsync();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting calendar-holiday associations for calendar {CalendarId}", calendarId);
                throw;
            }
        }

        public async Task RemoveHolidaysByCalendarIdAsync(Guid calendarId)
        {
            var calendarHolidays = await _context.CalendarHolidays
                .Where(ch => ch.CalendarId == calendarId)
                .ToListAsync();

            if (calendarHolidays.Any())
            {
                _logger.LogInformation("Removing {Count} holidays for calendar {CalendarId}", calendarHolidays.Count, calendarId);
                _context.CalendarHolidays.RemoveRange(calendarHolidays);
                await _context.SaveChangesAsync();
            }
            else
            {
                _logger.LogInformation("No holidays to remove for calendar {CalendarId}", calendarId);
            }
        }

        public async Task<IEnumerable<Holiday>> GetHolidaysByCalendarIdAsync(Guid calendarId)
        {
            var holidays = await _context.CalendarHolidays
                .Where(ch => ch.CalendarId == calendarId)
                .Include(ch => ch.Holiday) // Ensure the related Holiday entity is included
                .Select(ch => ch.Holiday)
                .ToListAsync();

            _logger.LogInformation("Fetched {Count} holidays for calendar {CalendarId}", holidays.Count, calendarId);
            return holidays;
        }


        public async Task AddRangeAsync(IEnumerable<CalendarHoliday> calendarHolidays)
        {
            try
            {
                await _context.CalendarHolidays.AddRangeAsync(calendarHolidays);
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding range of calendar-holiday associations");
                throw;
            }
        }
    }
}