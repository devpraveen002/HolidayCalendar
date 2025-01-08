using HolidayCalendar.src.HolidayCalendar.Core.Entities;
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
    //Task<Calendar> GetByIdAsync(int id);
    //Task<Calendar> GetDefaultCalendarAsync();
    //Task<Calendar> GetByShareableLinkAsync(string link);
    //Task<Calendar> CreateAsync(Calendar calendar);
    //Task UpdateAsync(Calendar calendar);
    //Task DeleteAsync(int id);
    //Task<IEnumerable<Calendar>> GetUserCalendarsAsync(string userId);
    //Task<Calendar> CreateCalendarAsync(Calendar calendar);
}
