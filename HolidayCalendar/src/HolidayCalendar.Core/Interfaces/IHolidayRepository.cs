using HolidayCalendar.src.HolidayCalendar.Core.Entities;

namespace HolidayCalendar.src.HolidayCalendar.Core.Interfaces
{
    public interface IHolidayRepository
    {
        Task<Holiday> GetByIdAsync(Guid id);
        Task<IEnumerable<Holiday>> GetDefaultHolidaysAsync();
        Task<Holiday> CreateAsync(Holiday holiday);
        Task UpdateAsync(Holiday holiday);
        Task DeleteAsync(Guid id);
        Task<IEnumerable<Holiday>> GetHolidaysByCalendarIdAsync(Guid calendarId);
    }
}
