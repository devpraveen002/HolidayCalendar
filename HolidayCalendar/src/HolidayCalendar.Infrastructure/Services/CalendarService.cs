using HolidayCalendar.src.HolidayCalendar.Core.DTOs;
using HolidayCalendar.src.HolidayCalendar.Core.Entities;
using HolidayCalendar.src.HolidayCalendar.Core.Interfaces;
using HolidayCalendar.src.HolidayCalendar.Core.Services;


namespace HolidayCalendar.src.HolidayCalendar.Infrastructure.Services;

public class CalendarService : ICalendarService
{
    private readonly ICalendarRepository _calendarRepository;
    private readonly IHolidayRepository _holidayRepository;
    private readonly ICalendarHolidayRepository _calendarHolidayRepository;
    private readonly ILogger<CalendarService> _logger;

    public CalendarService(
        ICalendarRepository calendarRepository,
        IHolidayRepository holidayRepository,
        ICalendarHolidayRepository calendarHolidayRepository,
        ILogger<CalendarService> logger)
    {
        _calendarRepository = calendarRepository;
        _holidayRepository = holidayRepository;
        _calendarHolidayRepository = calendarHolidayRepository;
        _logger = logger;
    }

    public async Task<CalendarDto> GetCalendarByIdAsync(Guid id)
    {
        try
        {
            var calendar = await _calendarRepository.GetByIdAsync(id);
            if (calendar == null) return null;

            var holidays = await _holidayRepository.GetHolidaysByCalendarIdAsync(id);
            return new CalendarDto
            {
                Calendar = calendar,
                Holidays = holidays.ToList(),
                ShareableLink = calendar.ShareableLink
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting calendar by id {Id}", id);
            throw;
        }
    }

    public async Task<CalendarDto> GetDefaultCalendarAsync()
    {
        try
        {
            var calendar = await _calendarRepository.GetDefaultCalendarAsync();
            var holidays = await _holidayRepository.GetHolidaysByCalendarIdAsync(calendar.Id);
            return new CalendarDto
            {
                Calendar = calendar,
                Holidays = holidays.ToList(),
                ShareableLink = calendar.ShareableLink
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting default calendar");
            throw;
        }
    }

    public async Task<CalendarDto> GetByShareableLinkAsync(string link)
    {
        try
        {
            var calendar = await _calendarRepository.GetByShareableLinkAsync(link);
            if (calendar == null) return null;

            var holidays = await _holidayRepository.GetHolidaysByCalendarIdAsync(calendar.Id);
            return new CalendarDto
            {
                Calendar = calendar,
                Holidays = holidays.ToList(),
                ShareableLink = link
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting calendar by shareable link");
            throw;
        }
    }

    public async Task<IEnumerable<CalendarDto>> GetUserCalendarsAsync(string userId)
    {
        try
        {
            // Convert string userId to long for repository call
            if (!long.TryParse(userId, out long userIdLong))
            {
                throw new ArgumentException("Invalid user ID format", nameof(userId));
            }

            var calendars = await _calendarRepository.GetUserCalendarsAsync(userIdLong);
            var calendarDtos = new List<CalendarDto>();

            foreach (var calendar in calendars)
            {
                var holidays = await _holidayRepository.GetHolidaysByCalendarIdAsync(calendar.Id);
                calendarDtos.Add(new CalendarDto
                {
                    Calendar = calendar,
                    Holidays = holidays.ToList(),
                    ShareableLink = calendar.ShareableLink
                });
            }

            return calendarDtos;
        }
        catch (ArgumentException)
        {
            _logger.LogError("Invalid user ID format: {UserId}", userId);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting user calendars for user {UserId}", userId);
            throw;
        }
    }

    public async Task<CalendarDto> CreateUserCalendarAsync(string userId, string name)
    {
        using var transaction = await _calendarRepository.BeginTransactionAsync();
        try
        {
            var userCalendar = new Calendar
            {
                Id = Guid.NewGuid(),
                Name = name,
                IsDefault = false,
                ShareableLink = Guid.NewGuid().ToString(),
                CreatedAt = DateTime.UtcNow,
                CreatedBy = long.Parse(userId)
            };

            var defaultHolidays = await _holidayRepository.GetDefaultHolidaysAsync();
            userCalendar = await _calendarRepository.CreateAsync(userCalendar);

            foreach (var holiday in defaultHolidays)
            {
                await _calendarHolidayRepository.CreateAsync(new CalendarHoliday
                {
                    Id = Guid.NewGuid(),
                    CalendarId = userCalendar.Id,
                    HolidayId = holiday.Id,
                    CreatedAt = DateTime.UtcNow,
                    CreatedBy = long.Parse(userId)
                });
            }

            await transaction.CommitAsync();
            return await GetCalendarByIdAsync(userCalendar.Id);
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            _logger.LogError(ex, "Error creating calendar for user {UserId}", userId);
            throw;
        }
    }

    public async Task<CalendarDto> UpdateCalendarAsync(Calendar calendar)
    {
        using var transaction = await _calendarRepository.BeginTransactionAsync();
        try
        {
            await _calendarRepository.UpdateAsync(calendar);
            await transaction.CommitAsync();
            return await GetCalendarByIdAsync(calendar.Id);
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            _logger.LogError(ex, "Error updating calendar");
            throw;
        }
    }

    public async Task DeleteCalendarAsync(Guid id)
    {
        using var transaction = await _calendarRepository.BeginTransactionAsync();
        try
        {
            await _calendarRepository.DeleteAsync(id);
            await transaction.CommitAsync();
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            _logger.LogError(ex, "Error deleting calendar");
            throw;
        }
    }

    public async Task<CalendarDto> AddHolidayAsync(Guid calendarId, Holiday holiday)
    {
        using var transaction = await _calendarRepository.BeginTransactionAsync();
        try
        {
            var calendar = await _calendarRepository.GetByIdAsync(calendarId);
            if (calendar == null)
            {
                throw new ArgumentException("Calendar not found", nameof(calendarId));
            }

            holiday = await _holidayRepository.CreateAsync(holiday);

            await _calendarHolidayRepository.CreateAsync(new CalendarHoliday
            {
                Id = Guid.NewGuid(),
                CalendarId = calendarId,
                HolidayId = holiday.Id,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = holiday.CreatedBy
            });

            await transaction.CommitAsync();
            return await GetCalendarByIdAsync(calendarId);
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            _logger.LogError(ex, "Error adding holiday to calendar {CalendarId}", calendarId);
            throw;
        }
    }
}
