using HolidayCalendar.src.HolidayCalendar.Core.Entities;
using HolidayCalendar.src.HolidayCalendar.Core.Interfaces;
using HolidayCalendar.src.HolidayCalendar.Infrastructure.Data;
using HolidayCalendar.src.HolidayCalendar.Web.ViewModels;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace HolidayCalendar.src.HolidayCalendar.Infrastructure.Repositories;

public class CalendarRepository : ICalendarRepository
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<CalendarRepository> _logger;

    public CalendarRepository(ApplicationDbContext context, ILogger<CalendarRepository> logger)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<IDbContextTransaction> BeginTransactionAsync()
    {
        return await _context.Database.BeginTransactionAsync();
    }

    public async Task<Calendar> GetByIdAsync(Guid id)
    {
        try
        {
            return await _context.Calendars
                .FirstOrDefaultAsync(c => c.Id == id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting calendar by id {Id}", id);
            throw;
        }
    }

    public async Task<Calendar> GetDefaultCalendarAsync()
    {
        try
        {
            var calendar = await _context.Calendars
                .FirstOrDefaultAsync(c => c.IsDefault);

            if (calendar == null)
            {
                throw new Exception("Default calendar not found. Please ensure the database is properly seeded.");
            }

            return calendar;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting default calendar");
            throw;
        }
    }

    public async Task<Calendar> GetByShareableLinkAsync(string link)
    {
        try
        {
            return await _context.Calendars
                .FirstOrDefaultAsync(c => c.ShareableLink == link);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting calendar by shareable link");
            throw;
        }
    }

    public async Task<Calendar> CreateAsync(Calendar calendar)
    {
        try
        {
            _context.Calendars.Add(calendar);
            await _context.SaveChangesAsync();
            return calendar;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating calendar");
            throw;
        }
    }

    public async Task UpdateAsync(Calendar calendar)
    {
        try
        {
            _context.Entry(calendar).State = EntityState.Modified;
            await _context.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating calendar");
            throw;
        }
    }

    public async Task DeleteAsync(Guid id)
    {
        try
        {
            var calendar = await GetByIdAsync(id);
            if (calendar != null)
            {
                _context.Calendars.Remove(calendar);
                await _context.SaveChangesAsync();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting calendar");
            throw;
        }
    }

    public async Task<IEnumerable<Calendar>> GetUserCalendarsAsync(long userId)
    {
        try
        {
            return await _context.UserCalendars
                .Where(uc => uc.UserId == userId)
                .Select(uc => uc.Calendar)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting user calendars");
            throw;
        }
    }

    public async Task<Event> AddEventAsync(Event @event)
    {
        try
        {
            _context.Events.Add(@event);
            await _context.SaveChangesAsync();
            return @event;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding event");
            throw;
        }
    }

    public async Task<Event> UpdateEventAsync(Event @event)
    {
        try
        {
            _context.Entry(@event).State = EntityState.Modified;
            await _context.SaveChangesAsync();
            return @event;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating event");
            throw;
        }
    }

    public async Task DeleteEventAsync(Guid eventId)
    {
        try
        {
            var @event = await _context.Events.FindAsync(eventId);
            if (@event != null)
            {
                _context.Events.Remove(@event);
                await _context.SaveChangesAsync();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting event");
            throw;
        }
    }

    public async Task<IEnumerable<Event>> GetEventsByCalendarIdAsync(Guid calendarId)
    {
        try
        {
            return await _context.Events
                .Where(e => e.CalendarId == calendarId)
                .OrderBy(e => e.StartDate)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting events for calendar");
            throw;
        }
    }

    public async Task<bool> IsUserAuthorizedForCalendarAsync(Guid calendarId, long userId)
    {
        try
        {
            return await _context.UserCalendars
                .AnyAsync(uc => uc.CalendarId == calendarId && uc.UserId == userId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking user authorization");
            throw;
        }
    }

    public async Task<IEnumerable<Calendar>> GetCalendarsByCountryAsync(string countryCode)
    {
        try
        {
            return await _context.Calendars
                .Where(c => c.CountryCode == countryCode || c.IsDefault)
                .OrderBy(c => c.Name)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting calendars by country code {CountryCode}", countryCode);
            throw;
        }
    }

    public async Task<Calendar> GetDefaultCalendarByCountryAsync(string countryCode)
    {
        try
        {
            var calendar = await _context.Calendars
                .FirstOrDefaultAsync(c => c.IsDefault && c.CountryCode == countryCode);

            if (calendar == null)
            {
                _logger.LogWarning("No default calendar found for country code: {CountryCode}", countryCode);
            }

            return calendar;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting default calendar for country {CountryCode}", countryCode);
            throw;
        }
    }

    public async Task<Calendar> GetUserCalendarByCountryAsync(long userId, string countryCode)
    {
        try
        {
            return await _context.UserCalendars
                .Where(uc => uc.UserId == userId)
                .Include(uc => uc.Calendar)
                .Select(uc => uc.Calendar)
                .FirstOrDefaultAsync(c => c.CountryCode == countryCode);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting user calendar for country {CountryCode} and user {UserId}",
                countryCode, userId);
            throw;
        }
    }
    public async Task<List<CountryViewModel>> GetDefaultCalendarCountriesAsync()
    {
        try
        {
            var countries = await _context.Calendars
                .Where(c => c.IsDefault && !string.IsNullOrEmpty(c.CountryCode))
                .Select(c => new CountryViewModel
                {
                    CountryCode = c.CountryCode,
                    CountryName = GetCountryName(c.CountryCode),
                    IsActive = true  // Assuming active by default, change as per your logic
                })
                .Distinct()
                .ToListAsync();

            return countries;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting default calendar countries");
            throw;
        }
    }
   
    public async Task CreateUserCalendarAsync(UserCalendar userCalendar)
    {
        try
        {
            _context.UserCalendars.Add(userCalendar);
            await _context.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating user calendar for user {UserId}", userCalendar.UserId);
            throw;
        }
    }

    private static string GetCountryName(string countryCode)
    {
        // You can implement this using a dictionary or database lookup
        // For now, returning a basic implementation
        var countries = new Dictionary<string, string>
            {
                {"US", "United States"},
                {"CA", "Canada"},
                {"AU", "Australia"},
                {"UK", "United Kingdom"},
                // Add more countries as needed
            };

                return countries.TryGetValue(countryCode, out string countryName)
                    ? countryName
                    : countryCode;
    }

    public async Task<IEnumerable<Calendar>> GetAllDefaultCalendarsAsync()
    {
        return await _context.Calendars
            .Where(c => c.IsDefault)
            .ToListAsync();
    }

    public async Task<IEnumerable<Calendar>> GetAllCalendarsAsync()
    {
        try
        {
            return await _context.Calendars.ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting all calendars");
            throw;
        }
    }
}
