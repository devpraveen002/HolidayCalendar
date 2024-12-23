using HolidayCalendar.src.HolidayCalendar.Core.Entities;

namespace HolidayCalendar.src.HolidayCalendar.Core.Interfaces;

public interface ICalendarRepository
{
    Task<Calendar> GetByIdAsync(int id);
    Task<Calendar> GetDefaultCalendarAsync();
    Task<Calendar> GetByShareableLinkAsync(string link);
    Task<Calendar> CreateAsync(Calendar calendar);
    Task UpdateAsync(Calendar calendar);
    Task DeleteAsync(int id);
    Task<IEnumerable<Calendar>> GetUserCalendarsAsync(string userId);
    Task<Calendar> CreateCalendarAsync(Calendar calendar);
}
