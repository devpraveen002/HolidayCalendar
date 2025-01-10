using HolidayCalendar.src.HolidayCalendar.Core.Entities;
using HolidayCalendar.src.HolidayCalendar.Web.ViewModels;
using Microsoft.EntityFrameworkCore.Storage;

namespace HolidayCalendar.src.HolidayCalendar.Core.Interfaces;

public interface ICalendarRepository
{
    Task<Calendar> GetByIdAsync(Guid id);
    Task<Calendar> GetDefaultCalendarAsync();
    Task<Calendar> GetByShareableLinkAsync(string link);
    Task<Calendar> CreateAsync(Calendar calendar);
    Task UpdateAsync(Calendar calendar);
    Task DeleteAsync(Guid id);
    Task<IEnumerable<Calendar>> GetUserCalendarsAsync(long userId);
    Task<IDbContextTransaction> BeginTransactionAsync();

    Task<Event> AddEventAsync(Event @event);
    Task<Event> UpdateEventAsync(Event @event);
    Task DeleteEventAsync(Guid eventId);
    Task<IEnumerable<Event>> GetEventsByCalendarIdAsync(Guid calendarId);
    Task<bool> IsUserAuthorizedForCalendarAsync(Guid calendarId, long userId);
    Task<IEnumerable<Calendar>> GetCalendarsByCountryAsync(string countryCode);
    Task AddCountryAsync(Country country);
    Task<Calendar> GetDefaultCalendarByCountryAsync(string countryCode);
    Task<Calendar> GetUserCalendarByCountryAsync(long userId, string countryCode);
    Task<List<CountryViewModel>> GetDefaultCalendarCountriesAsync();
    Task CreateUserCalendarAsync(UserCalendar userCalendar);
    Task<IEnumerable<Calendar>> GetAllDefaultCalendarsAsync();
    Task<IEnumerable<Calendar>> GetAllCalendarsAsync();
    Task UpdateUserCalendarCountryAsync(Guid calendarId, string countryCode);
}
