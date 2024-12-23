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

    public async Task<Calendar> CreateUserCalendarAsync(string userId)
    {
        try
        {
            var defaultCalendar = await GetDefaultCalendarAsync();
            var userCalendar = new Calendar
            {
                Name = "My Calendar",
                UserId = userId,
                IsDefault = false,
                ShareableLink = Guid.NewGuid().ToString()
            };

            userCalendar.Holidays = defaultCalendar.Holidays.Select(h => new Holiday
            {
                Name = h.Name,
                Date = h.Date,
                IsFixedHoliday = h.IsFixedHoliday,
                IsWeekendAdjustable = h.IsWeekendAdjustable
            }).ToList();

            return await _calendarRepository.CreateAsync(userCalendar);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating user calendar for user {UserId}", userId);
            throw;
        }
    }

    public async Task<Calendar> AddHolidayAsync(int calendarId, Holiday holiday)
    {
        try
        {
            var calendar = await GetCalendarByIdAsync(calendarId);
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

    public async Task<string> GenerateShareableLinkAsync(int calendarId)
    {
        try
        {
            var calendar = await GetCalendarByIdAsync(calendarId);
            calendar.ShareableLink = Guid.NewGuid().ToString();
            await _calendarRepository.UpdateAsync(calendar);
            return calendar.ShareableLink;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating shareable link for calendar {CalendarId}", calendarId);
            throw;
        }
    }

    public async Task<Calendar> GetCalendarByShareableLinkAsync(string link)
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

    public async Task<Calendar> CreateCalendarAsync(Calendar calendar)
    {
        try
        {
            if (calendar.Id == 0)
            {
                var defaultCalendar = await GetDefaultCalendarAsync();
                calendar.Holidays = defaultCalendar.Holidays.Select(h => new Holiday
                {
                    Name = h.Name,
                    Date = h.Date,
                    IsFixedHoliday = h.IsFixedHoliday,
                    IsWeekendAdjustable = h.IsWeekendAdjustable
                }).ToList();
            }

            return await _calendarRepository.CreateAsync(calendar);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating calendar");
            throw;
        }
    }
}

