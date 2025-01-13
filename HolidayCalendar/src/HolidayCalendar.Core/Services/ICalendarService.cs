using HolidayCalendar.src.HolidayCalendar.Core.DTOs;
using HolidayCalendar.src.HolidayCalendar.Core.Entities;
using HolidayCalendar.src.HolidayCalendar.Web.ViewModels;

namespace HolidayCalendar.src.HolidayCalendar.Core.Services;

public interface ICalendarService
{
    Task<CalendarDto> UpdateDefaultCalendarAsync(Calendar calendar, long userId);
    Task<CalendarDto> GetCalendarByIdAsync(Guid id);
    Task<CalendarDto> GetDefaultCalendarAsync();
    Task<CalendarDto> GetByShareableLinkAsync(string link);
    Task<IEnumerable<CalendarDto>> GetUserCalendarsAsync(string userId);
    Task<CalendarDto> CreateUserCalendarAsync(string userId, string name, string countryCode);
    Task<CalendarDto> UpdateCalendarAsync(Guid calendarId, string name, string countryCode, long userId, bool isAdmin);
    Task AddCountryAsync(Country country);
    Task DeleteCalendarAsync(Guid id);
    Task<CalendarDto> AddHolidayAsync(Guid calendarId, Holiday holiday);
    Task<Event> AddEventAsync(Guid calendarId, Event @event);
    Task<Event> UpdateEventAsync(Guid calendarId, Event @event, long userId, bool isAdmin);
    Task DeleteEventAsync(Guid calendarId, Guid eventId, long userId, bool isAdmin);
    Task<IEnumerable<Event>> GetEventsByCalendarIdAsync(Guid calendarId);
    Task<IEnumerable<Holiday>> GetHolidaysByCalendarIdAsync(Guid calendarId);

    // Calendar sharing
    Task<string> GenerateShareableLinkAsync(Guid calendarId);
    Task<IEnumerable<CalendarDto>> GetAllDefaultCalendarsAsync();
    Task<bool> IsCalendarAccessibleByUserAsync(Guid calendarId, long userId);
    Task<IEnumerable<CalendarDto>> GetAllCalendarsAsync();
    Task CreateDefaultCalendarAsync(CreateCalendarViewModel model);
    Task<CalendarDto> GetUserCalendarByIdAsync(Guid id, string userId);


    // Export operations
    Task<byte[]> ExportMonthToExcelAsync(Guid calendarId, DateTime month);
    Task<byte[]> ExportMonthToPdfAsync(Guid calendarId, DateTime month);
    Task<byte[]> ExportMonthToCsvAsync(Guid calendarId, DateTime month);
    Task<byte[]> ExportMonthToIcsAsync(Guid calendarId, DateTime month);

    // Yearly exports
    Task<byte[]> ExportYearToExcelAsync(Guid calendarId, int year);
    Task<byte[]> ExportYearToPdfAsync(Guid calendarId, int year);
    Task<byte[]> ExportYearToCsvAsync(Guid calendarId, int year);
    Task<byte[]> ExportYearToIcsAsync(Guid calendarId, int year);

    // Country-specific calendars
    Task<IEnumerable<CalendarDto>> GetCalendarsByCountryAsync(string countryCode);

    Task<CalendarDto> GetDefaultCalendarByCountryAsync(string countryCode);
    Task<CalendarDto> GetUserCalendarAsync(string userId, string countryCode);
    Task<List<CountryViewModel>> GetDefaultCalendarCountriesAsync();
    Task CreateUserCalendarAsync(UserCalendar userCalendar);
    Task UpdateCalendarHolidaysAsync(Guid id, string selectedCountry, long userId);
}
