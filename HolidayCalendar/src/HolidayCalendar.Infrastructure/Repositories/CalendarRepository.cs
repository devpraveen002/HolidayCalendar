using HolidayCalendar.src.HolidayCalendar.Core.Entities;
using HolidayCalendar.src.HolidayCalendar.Core.Interfaces;
using HolidayCalendar.src.HolidayCalendar.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

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

    public async Task<Calendar> GetByIdAsync(int id)
    {
        try
        {
            var calendar = await _context.Calendars
                .Include(c => c.Holidays)
                .FirstOrDefaultAsync(c => c.Id == id);
            return calendar;
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
                .Include(c => c.Holidays)
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
            var calendar = await _context.Calendars
                .Include(c => c.Holidays)
                .FirstOrDefaultAsync(c => c.ShareableLink == link);
            return calendar;
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

    public async Task DeleteAsync(int id)
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
    public async Task<IEnumerable<Calendar>> GetUserCalendarsAsync(string userId)
    {
        return await _context.Calendars
            .Include(c => c.Holidays)
            .Where(c => c.UserId == userId && !c.IsDefault)
            .ToListAsync();
    }

    public async Task<Calendar> CreateCalendarAsync(Calendar calendar)
    {
        _context.Calendars.Add(calendar);
        await _context.SaveChangesAsync();
        return calendar;
    }
}
