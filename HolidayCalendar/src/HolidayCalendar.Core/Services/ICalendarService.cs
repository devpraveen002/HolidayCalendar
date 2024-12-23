using HolidayCalendar.src.HolidayCalendar.Core.Entities;

namespace HolidayCalendar.src.HolidayCalendar.Core.Services;

public interface ICalendarService
{
    Task<Calendar> GetCalendarByIdAsync(int id);
    Task<Calendar> GetDefaultCalendarAsync();
    Task<Calendar> CreateUserCalendarAsync(string userId);
    Task<Calendar> AddHolidayAsync(int calendarId, Holiday holiday);
    Task<string> GenerateShareableLinkAsync(int calendarId);
    Task<Calendar> GetCalendarByShareableLinkAsync(string link);
    Task<Calendar> CreateCalendarAsync(Calendar calendar);
    Task<IEnumerable<Calendar>> GetUserCalendarsAsync(string userId);
}
