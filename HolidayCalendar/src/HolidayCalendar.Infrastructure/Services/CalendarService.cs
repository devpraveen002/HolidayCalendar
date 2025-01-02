using HolidayCalendar.src.HolidayCalendar.Core.Entities;
using HolidayCalendar.src.HolidayCalendar.Core.Interfaces;
using HolidayCalendar.src.HolidayCalendar.Core.Services;


namespace HolidayCalendar.src.HolidayCalendar.Infrastructure.Services;

public class CalendarService : ICalendarService
{
    private readonly ICalendarRepository _calendarRepository;
    private readonly ILogger<CalendarService> _logger;

    public CalendarService(
        ICalendarRepository calendarRepository,
        ILogger<CalendarService> logger)
    {
        _calendarRepository = calendarRepository;
        _logger = logger;
    }

    public async Task<Calendar> GetCalendarByIdAsync(int id)
    {
        try
        {
            return await _calendarRepository.GetByIdAsync(id);
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
            return await _calendarRepository.GetDefaultCalendarAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting default calendar");
            throw;
        }
    }
        public async Task<Calendar> CreateUserCalendarAsync(string userId, string name)
        {
            try
            {
                var defaultCalendar = await GetDefaultCalendarAsync();
                var userCalendar = new Calendar
                {
                    Name = name,
                    UserId = userId,
                    IsDefault = false,
                    ShareableLink = Guid.NewGuid().ToString(),
                    Holidays = await Task.WhenAll(defaultCalendar.Holidays.Select(async h => new Holiday
                    {
                        Name = h.Name,
                        Date = h.Date,
                        IsFixedHoliday = h.IsFixedHoliday,
                        IsWeekendAdjustable = h.IsWeekendAdjustable
                    }))
                };

                return await _calendarRepository.CreateAsync(userCalendar);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating calendar for user {UserId}", userId);
                throw;
            }
        }

        public async Task<Calendar> UpdateCalendarAsync(Calendar calendar)
    {
        try
        {
            await _calendarRepository.UpdateAsync(calendar);
            return calendar;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating calendar {CalendarId}", calendar.Id);
            throw;
        }
    }

    public async Task DeleteCalendarAsync(int id)
    {
        try
        {
            await _calendarRepository.DeleteAsync(id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting calendar {CalendarId}", id);
            throw;
        }
    }

    public async Task<Calendar> GetByShareableLinkAsync(string link)
    {
        try
        {
            return await _calendarRepository.GetByShareableLinkAsync(link);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting calendar by shareable link");
            throw;
        }
    }

    public async Task<IEnumerable<Calendar>> GetUserCalendarsAsync(string userId)
    {
        try
        {
            return await _calendarRepository.GetUserCalendarsAsync(userId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting calendars for user {UserId}", userId);
            throw;
        }
    }

        public async Task<Calendar> AddHolidayAsync(int calendarId, Holiday holiday)
        {
            try
            {
                var calendar = await _calendarRepository.GetByIdAsync(calendarId);
                if (calendar == null)
                {
                    throw new ArgumentException("Calendar not found", nameof(calendarId));
                }

                calendar.Holidays.Add(holiday);
                await _calendarRepository.UpdateAsync(calendar);
                return calendar;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding holiday to calendar {CalendarId}", calendarId);
                throw;
            }
        }
    }

