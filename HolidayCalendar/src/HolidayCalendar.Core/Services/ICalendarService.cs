using HolidayCalendar.src.HolidayCalendar.Core.Entities;

namespace HolidayCalendar.src.HolidayCalendar.Core.Services;

public interface ICalendarService
{
    Task<Calendar> GetCalendarByIdAsync(int id);
    Task<Calendar> GetDefaultCalendarAsync();
    Task<Calendar> GetByShareableLinkAsync(string link);
    Task<IEnumerable<Calendar>> GetUserCalendarsAsync(string userId);
    Task<Calendar> CreateUserCalendarAsync(string userId, string name);
    Task<Calendar> UpdateCalendarAsync(Calendar calendar);
    Task DeleteCalendarAsync(int id);
    Task<Calendar> AddHolidayAsync(int calendarId, Holiday holiday);
}
