using HolidayCalendar.src.HolidayCalendar.Core.DTOs;
using HolidayCalendar.src.HolidayCalendar.Core.Entities;

namespace HolidayCalendar.src.HolidayCalendar.Core.Services;

public interface ICalendarService
{
    Task<CalendarDto> GetCalendarByIdAsync(Guid id);
    Task<CalendarDto> GetDefaultCalendarAsync();
    Task<CalendarDto> GetByShareableLinkAsync(string link);
    Task<IEnumerable<CalendarDto>> GetUserCalendarsAsync(string userId);
    Task<CalendarDto> CreateUserCalendarAsync(string userId, string name);
    Task<CalendarDto> UpdateCalendarAsync(Calendar calendar);
    Task DeleteCalendarAsync(Guid id);
    Task<CalendarDto> AddHolidayAsync(Guid calendarId, Holiday holiday);
}
