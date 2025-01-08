using HolidayCalendar.src.HolidayCalendar.Core.Entities;
using HolidayCalendar.src.HolidayCalendar.Core.Interfaces;
using HolidayCalendar.src.HolidayCalendar.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace HolidayCalendar.src.HolidayCalendar.Infrastructure.Repositories;

public class HolidayRepository : IHolidayRepository
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<HolidayRepository> _logger;

    public HolidayRepository(ApplicationDbContext context, ILogger<HolidayRepository> logger)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<Holiday> GetByIdAsync(Guid id)
    {
        try
        {
            return await _context.Holidays
                .FirstOrDefaultAsync(h => h.Id == id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting holiday by id {Id}", id);
            throw;
        }
    }

    public async Task<IEnumerable<Holiday>> GetDefaultHolidaysAsync()
    {
        try
        {
            return await _context.Holidays
                .Where(h => h.IsDefault)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting default holidays");
            throw;
        }
    }

    public async Task<Holiday> CreateAsync(Holiday holiday)
    {
        try
        {
            _context.Holidays.Add(holiday);
            await _context.SaveChangesAsync();
            return holiday;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating holiday");
            throw;
        }
    }

    public async Task UpdateAsync(Holiday holiday)
    {
        try
        {
            _context.Entry(holiday).State = EntityState.Modified;
            await _context.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating holiday");
            throw;
        }
    }

    public async Task DeleteAsync(Guid id)
    {
        try
        {
            var holiday = await GetByIdAsync(id);
            if (holiday != null)
            {
                _context.Holidays.Remove(holiday);
                await _context.SaveChangesAsync();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting holiday");
            throw;
        }
    }

    public async Task<IEnumerable<Holiday>> GetHolidaysByCalendarIdAsync(Guid calendarId)
    {
        try
        {
            return await _context.CalendarHolidays
                .Where(ch => ch.CalendarId == calendarId)
                .Select(ch => ch.Holiday)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting holidays for calendar {CalendarId}", calendarId);
            throw;
        }
    }

}
