using HolidayCalendar.src.HolidayCalendar.Core.Entities;

namespace HolidayCalendar.src.HolidayCalendar.Core.Interfaces;

public interface ICalendarHolidayRepository
{
    Task<CalendarHoliday> CreateAsync(CalendarHoliday calendarHoliday);
    Task DeleteAsync(Guid calendarId, Guid holidayId);
    Task<IEnumerable<CalendarHoliday>> GetByCalendarIdAsync(Guid calendarId);
    Task RemoveHolidaysByCalendarIdAsync(Guid calendarId);
    Task<IEnumerable<Holiday>> GetHolidaysByCalendarIdAsync(Guid calendarId);
}
