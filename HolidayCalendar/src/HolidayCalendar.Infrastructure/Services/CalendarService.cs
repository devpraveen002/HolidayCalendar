using HolidayCalendar.src.HolidayCalendar.Core.DTOs;
using HolidayCalendar.src.HolidayCalendar.Core.Entities;
using HolidayCalendar.src.HolidayCalendar.Core.Interfaces;
using HolidayCalendar.src.HolidayCalendar.Core.Services;
using iText.Kernel.Pdf;
using iText.Layout;
using iText.Layout.Element;
using Microsoft.AspNetCore.Identity;
using OfficeOpenXml;
using OfficeOpenXml.Style;
using System.Text;
using iText.Layout.Properties;
using iText.IO.Font.Constants;
using iText.Kernel.Font;
using HolidayCalendar.src.HolidayCalendar.Web.ViewModels;


namespace HolidayCalendar.src.HolidayCalendar.Infrastructure.Services;

public class CalendarService : ICalendarService
{
    private readonly ICalendarRepository _calendarRepository;
    private readonly IHolidayRepository _holidayRepository;
    private readonly ICalendarHolidayRepository _calendarHolidayRepository;
    private readonly UserManager<User> _userManager; // Add this field
    private readonly ILogger<CalendarService> _logger;

    public CalendarService(
        ICalendarRepository calendarRepository,
        IHolidayRepository holidayRepository,
        ICalendarHolidayRepository calendarHolidayRepository,
        UserManager<User> userManager, // Add this parameter
        ILogger<CalendarService> logger)
    {
        _calendarRepository = calendarRepository;
        _holidayRepository = holidayRepository;
        _calendarHolidayRepository = calendarHolidayRepository;
        _userManager = userManager;
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
            if (!long.TryParse(userId, out long userIdLong))
            {
                throw new ArgumentException("Invalid user ID format", nameof(userId));
            }

            var calendars = await _calendarRepository.GetUserCalendarsAsync(userIdLong);
            var calendarDtos = new List<CalendarDto>();

            foreach (var calendar in calendars)
            {
                // Fetch the updated list of holidays
                var holidays = await _calendarHolidayRepository.GetHolidaysByCalendarIdAsync(calendar.Id);

                calendarDtos.Add(new CalendarDto
                {
                    Calendar = calendar,
                    Holidays = holidays.ToList(), // Ensure updated holidays are fetched
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

    public async Task CreateUserCalendarAsync(UserCalendar userCalendar)
    {
        using var transaction = await _calendarRepository.BeginTransactionAsync();
        try
        {
            var calendar = new Calendar
            {
                Id = userCalendar.CalendarId,
                Name = $"Calendar for User {userCalendar.UserId}",
                CountryCode = userCalendar.CountryCode, // Ensure the country code is included
                IsDefault = false,
                ShareableLink = Guid.NewGuid().ToString(),
                CreatedAt = DateTime.UtcNow,
                CreatedBy = userCalendar.UserId
            };

            await _calendarRepository.CreateAsync(calendar);
            await _calendarRepository.CreateUserCalendarAsync(userCalendar);

            // Copy country-specific holidays
            var defaultCalendar = await _calendarRepository.GetDefaultCalendarByCountryAsync(userCalendar.CountryCode);
            if (defaultCalendar != null)
            {
                var countryHolidays = await _holidayRepository.GetHolidaysByCalendarIdAsync(defaultCalendar.Id);
                foreach (var holiday in countryHolidays)
                {
                    await _calendarHolidayRepository.CreateAsync(new CalendarHoliday
                    {
                        Id = Guid.NewGuid(),
                        CalendarId = userCalendar.CalendarId,
                        HolidayId = holiday.Id,
                        CreatedAt = DateTime.UtcNow,
                        CreatedBy = userCalendar.UserId
                    });
                }
            }

            await transaction.CommitAsync();
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            _logger.LogError(ex, "Error creating user calendar");
            throw;
        }
    }

    public async Task<CalendarDto> CreateUserCalendarAsync(string userId, string name, string countryCode)
    {
        using var transaction = await _calendarRepository.BeginTransactionAsync();
        try
        {
            if (!long.TryParse(userId, out long userIdLong))
            {
                throw new ArgumentException("Invalid user ID format", nameof(userId));
            }

            // Create the calendar
            var calendar = new Calendar
            {
                Id = Guid.NewGuid(),
                Name = name,
                CountryCode = countryCode,
                IsDefault = false,
                IsDefaultCountryCalendar = false,
                IsUserCreated = true,
                ShareableLink = Guid.NewGuid().ToString(),
                CreatedAt = DateTime.UtcNow,
                CreatedBy = userIdLong
            };

            calendar = await _calendarRepository.CreateAsync(calendar);

            // Create UserCalendar
            var userCalendar = new UserCalendar
            {
                Id = Guid.NewGuid(),
                UserId = userIdLong,
                CalendarId = calendar.Id,
                CountryCode = countryCode, // Pass CountryCode
                CreatedAt = DateTime.UtcNow,
                CreatedBy = userIdLong
            };

            await _calendarRepository.CreateUserCalendarAsync(userCalendar);

            // Copy country-specific holidays
            var defaultCalendar = await _calendarRepository.GetDefaultCalendarByCountryAsync(countryCode);
            if (defaultCalendar != null)
            {
                var countryHolidays = await _holidayRepository.GetHolidaysByCalendarIdAsync(defaultCalendar.Id);
                foreach (var holiday in countryHolidays)
                {
                    await _calendarHolidayRepository.CreateAsync(new CalendarHoliday
                    {
                        Id = Guid.NewGuid(),
                        CalendarId = calendar.Id,
                        HolidayId = holiday.Id,
                        CreatedAt = DateTime.UtcNow,
                        CreatedBy = userIdLong
                    });
                }
            }

            await transaction.CommitAsync();
            return await GetCalendarByIdAsync(calendar.Id);
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            _logger.LogError(ex, "Error creating calendar for user {UserId}", userId);
            throw;
        }
    }



    public async Task UpdateCalendarHolidaysAsync(Guid calendarId, string countryCode, long userId)
    {
        using var transaction = await _calendarRepository.BeginTransactionAsync();
        try
        {
            _logger.LogInformation("Starting holiday update for calendar {CalendarId}", calendarId);

            // Remove existing holidays
            _logger.LogInformation("Removing existing holidays for calendar {CalendarId}", calendarId);
            await _calendarHolidayRepository.RemoveHolidaysByCalendarIdAsync(calendarId);

            // Fetch default calendar for the new country
            _logger.LogInformation("Fetching default calendar for country {CountryCode}", countryCode);
            var defaultCalendar = await _calendarRepository.GetDefaultCalendarByCountryAsync(countryCode);

            if (defaultCalendar != null)
            {
                _logger.LogInformation("Fetching holidays from default calendar {DefaultCalendarId} for country {CountryCode}",
                    defaultCalendar.Id, countryCode);

                var countryHolidays = await _holidayRepository.GetHolidaysByCalendarIdAsync(defaultCalendar.Id);
                _logger.LogInformation("Found {HolidayCount} holidays for country {CountryCode}", countryHolidays.Count(), countryCode);

                // Add holidays for the new country
                foreach (var holiday in countryHolidays)
                {
                    _logger.LogInformation("Adding holiday {HolidayId} to calendar {CalendarId}", holiday.Id, calendarId);

                    await _calendarHolidayRepository.CreateAsync(new CalendarHoliday
                    {
                        Id = Guid.NewGuid(),
                        CalendarId = calendarId,
                        HolidayId = holiday.Id,
                        CreatedAt = DateTime.UtcNow,
                        CreatedBy = userId
                    });
                }
            }
            else
            {
                _logger.LogWarning("No default calendar found for country {CountryCode}", countryCode);
            }

            await transaction.CommitAsync();
            _logger.LogInformation("Finished holiday update for calendar {CalendarId}", calendarId);
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            _logger.LogError(ex, "Error updating holidays for calendar {CalendarId}", calendarId);
            throw;
        }
    }




    public async Task<CalendarDto> GetDefaultCalendarByCountryAsync(string countryCode)
    {
        try
        {
            var calendar = await _calendarRepository.GetDefaultCalendarByCountryAsync(countryCode);

            if (calendar == null)
            {
                _logger.LogWarning("No default calendar found for country code: {CountryCode}", countryCode);
                return null;
            }

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
            _logger.LogError(ex, "Error getting default calendar for country {CountryCode}", countryCode);
            throw;
        }
    }

    public async Task<CalendarDto> GetUserCalendarAsync(string userId, string countryCode)
    {
        try
        {
            if (!long.TryParse(userId, out long userIdLong))
            {
                throw new ArgumentException("Invalid user ID format", nameof(userId));
            }

            var calendar = await _calendarRepository.GetUserCalendarByCountryAsync(userIdLong, countryCode);
            if (calendar == null) return null;

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
            _logger.LogError(ex, "Error getting user calendar for country {CountryCode}", countryCode);
            throw;
        }
    }

    public async Task<List<CountryViewModel>> GetDefaultCalendarCountriesAsync()
    {
        try
        {
            var countries = await _calendarRepository.GetDefaultCalendarCountriesAsync();
            return countries.Select(c => new CountryViewModel
            {
                CountryCode = c.CountryCode,
                CountryName = c.CountryName,
                // Add any other properties needed for the view model
            }).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting default calendar countries");
            throw;
        }
    }
    public async Task<CalendarDto> UpdateCalendarAsync(Guid calendarId, string name, string countryCode, long userId, bool isAdmin)
    {
        using var transaction = await _calendarRepository.BeginTransactionAsync();
        try
        {
            var calendar = await _calendarRepository.GetByIdAsync(calendarId);
            if (calendar == null)
            {
                throw new Exception($"Calendar with ID {calendarId} not found.");
            }

            // Authorization Check
            if (!isAdmin && calendar.CreatedBy != userId)
            {
                throw new UnauthorizedAccessException("You are not authorized to edit this calendar.");
            }

            // Update properties
            calendar.Name = name;
            calendar.CountryCode = countryCode;
            calendar.ModifiedAt = DateTime.UtcNow;
            calendar.ModifiedBy = userId;

            await _calendarRepository.UpdateAsync(calendar);
            await transaction.CommitAsync();

            return await GetCalendarByIdAsync(calendar.Id);
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            _logger.LogError(ex, "Error updating calendar with ID {CalendarId}", calendarId);
            throw;
        }
    }

    public async Task CreateDefaultCalendarAsync(CreateCalendarViewModel model)
    {
        using var transaction = await _calendarRepository.BeginTransactionAsync();
        try
        {
            // Ensure only admin can create default calendars
            var user = await _userManager.FindByIdAsync(model.CreatedBy.ToString());
            if (user == null || !await _userManager.IsInRoleAsync(user, "Admin"))
            {
                throw new UnauthorizedAccessException("Only administrators can create default calendars");
            }

            var calendar = new Calendar
            {
                Id = Guid.NewGuid(),
                Name = model.Name,
                IsDefault = model.IsDefault,
                CountryCode = model.CountryCode,
                ShareableLink = Guid.NewGuid().ToString(),
                CreatedAt = DateTime.UtcNow,
                CreatedBy = model.CreatedBy
            };

            await _calendarRepository.CreateAsync(calendar);
            await transaction.CommitAsync();
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            _logger.LogError(ex, "Error creating default calendar");
            throw;
        }
    }

    public async Task AddCountryAsync(Country country)
    {
        try
        {
            await _calendarRepository.AddCountryAsync(country);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding country with code {CountryCode}", country.CountryCode);
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

    public async Task UpdateUserCalendarCountryAsync(Guid calendarId, string countryCode)
    {
        try
        {
            await _calendarRepository.UpdateUserCalendarCountryAsync(calendarId, countryCode);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating country code for calendar {CalendarId}", calendarId);
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

    // Add these methods to CalendarService.cs

    public async Task<CalendarDto> UpdateDefaultCalendarAsync(Calendar calendar, long userId)
    {
        try
        {
            // Verify user is admin
            var isAdmin = await _userManager.IsInRoleAsync((await _userManager.FindByIdAsync(userId.ToString())), "Admin");
            if (!isAdmin)
            {
                throw new UnauthorizedAccessException("Only administrators can modify the default calendar");
            }

            using var transaction = await _calendarRepository.BeginTransactionAsync();
            try
            {
                calendar.ModifiedAt = DateTime.UtcNow;
                calendar.ModifiedBy = userId;
                await _calendarRepository.UpdateAsync(calendar);
                await transaction.CommitAsync();
                return await GetCalendarByIdAsync(calendar.Id);
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating default calendar");
            throw;
        }
    }

    public async Task<Event> AddEventAsync(Guid calendarId, Event @event)
    {
        try
        {
            using var transaction = await _calendarRepository.BeginTransactionAsync();
            try
            {
                var calendar = await _calendarRepository.GetByIdAsync(calendarId);
                if (calendar == null)
                    throw new ArgumentException("Calendar not found");

                @event.CalendarId = calendarId;
                @event.Id = Guid.NewGuid();
                @event.CreatedAt = DateTime.UtcNow;

                await _calendarRepository.AddEventAsync(@event);
                await transaction.CommitAsync();
                return @event;
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding event to calendar");
            throw;
        }
    }

    public async Task<IEnumerable<CalendarDto>> GetAllDefaultCalendarsAsync()
    {
        try
        {
            // Retrieve all default calendars
            var defaultCalendars = await _calendarRepository.GetAllDefaultCalendarsAsync();

            // Map to CalendarDto
            var calendarDtos = new List<CalendarDto>();

            foreach (var calendar in defaultCalendars)
            {
                // Retrieve holidays for the calendar
                var holidays = await _holidayRepository.GetHolidaysByCalendarIdAsync(calendar.Id);

                // Map CalendarDto
                calendarDtos.Add(new CalendarDto
                {
                    Calendar = calendar,
                    Holidays = holidays.ToList(),
                    ShareableLink = calendar.ShareableLink
                });
            }

            return calendarDtos;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving all default calendars");
            throw;
        }
    }

    public async Task<IEnumerable<Holiday>> GetHolidaysByCalendarIdAsync(Guid calendarId)
    {
        return await _calendarHolidayRepository.GetHolidaysByCalendarIdAsync(calendarId);
    }

    public async Task<string> GenerateShareableLinkAsync(Guid calendarId)
    {
        var calendar = await _calendarRepository.GetByIdAsync(calendarId);
        if (calendar == null)
            throw new ArgumentException("Calendar not found");

        calendar.ShareableLink = Guid.NewGuid().ToString();
        await _calendarRepository.UpdateAsync(calendar);
        return calendar.ShareableLink;
    }

    public async Task<Event> UpdateEventAsync(Guid calendarId, Event @event, long userId, bool isAdmin)
    {
        using var transaction = await _calendarRepository.BeginTransactionAsync();
        try
        {
            var calendar = await _calendarRepository.GetByIdAsync(calendarId);
            if (calendar == null)
                throw new ArgumentException("Calendar not found");

            // Authorization Check
            if (!isAdmin && calendar.CreatedBy != userId)
                throw new UnauthorizedAccessException("You are not authorized to edit this event.");

            @event.ModifiedAt = DateTime.UtcNow;
            await _calendarRepository.UpdateEventAsync(@event);

            await transaction.CommitAsync();
            return @event;
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            _logger.LogError(ex, "Error updating event for calendar {CalendarId}", calendarId);
            throw;
        }
    }


    public async Task DeleteEventAsync(Guid calendarId, Guid eventId, long userId, bool isAdmin)
    {
        using var transaction = await _calendarRepository.BeginTransactionAsync();
        try
        {
            var calendar = await _calendarRepository.GetByIdAsync(calendarId);
            if (calendar == null)
                throw new ArgumentException("Calendar not found");

            // Authorization Check
            if (!isAdmin && calendar.CreatedBy != userId)
                throw new UnauthorizedAccessException("You are not authorized to delete this event.");

            await _calendarRepository.DeleteEventAsync(eventId);

            await transaction.CommitAsync();
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            _logger.LogError(ex, "Error deleting event for calendar {CalendarId}", calendarId);
            throw;
        }
    }

    public async Task<CalendarDto> GetUserCalendarByIdAsync(Guid id, string userId)
    {
        var calendar = await _calendarRepository.GetByIdAsync(id);
        if (calendar == null || calendar.CreatedBy.ToString() != userId)
            return null;

        var holidays = await _holidayRepository.GetHolidaysByCalendarIdAsync(id);
        return new CalendarDto
        {
            Calendar = calendar,
            Holidays = holidays.ToList(),
            ShareableLink = calendar.ShareableLink
        };
    }


    public async Task<IEnumerable<Event>> GetEventsByCalendarIdAsync(Guid calendarId)
    {
        try
        {
            return await _calendarRepository.GetEventsByCalendarIdAsync(calendarId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting events for calendar");
            throw;
        }
    }

    public async Task<bool> IsCalendarAccessibleByUserAsync(Guid calendarId, long userId)
    {
        try
        {
            var calendar = await _calendarRepository.GetByIdAsync(calendarId);
            if (calendar == null)
                return false;

            if (calendar.IsDefault)
                return true;

            return await _calendarRepository.IsUserAuthorizedForCalendarAsync(calendarId, userId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking calendar access");
            throw;
        }
    }
    public async Task<IEnumerable<CalendarDto>> GetCalendarsByCountryAsync(string countryCode)
    {
        try
        {
            // This would be implemented based on your country-specific calendar logic
            var calendars = await _calendarRepository.GetCalendarsByCountryAsync(countryCode);
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
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting calendars by country");
            throw;
        }
    }

    public async Task<IEnumerable<CalendarDto>> GetAllCalendarsAsync()
    {
        try
        {
            var calendars = await _calendarRepository.GetAllCalendarsAsync();
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
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving all calendars");
            throw;
        }
    }

    public async Task<byte[]> ExportMonthToExcelAsync(Guid calendarId, DateTime month)
    {
        try
        {
            var calendar = await GetCalendarByIdAsync(calendarId);
            if (calendar == null)
                throw new ArgumentException("Calendar not found");

            var startDate = new DateTime(month.Year, month.Month, 1);
            var endDate = startDate.AddMonths(1).AddDays(-1);

            var holidays = calendar.Holidays
                .Where(h => h.Date >= startDate && h.Date <= endDate)
                .OrderBy(h => h.Date);

            var events = await GetEventsByCalendarIdAsync(calendarId);
            var monthEvents = events
                .Where(e => e.StartDate <= endDate && e.EndDate >= startDate)
                .OrderBy(e => e.StartDate);

            using (var package = new ExcelPackage())
            {
                var worksheet = package.Workbook.Worksheets.Add($"{month:MMMM yyyy}");

                // Headers
                worksheet.Cells["A1"].Value = "Date";
                worksheet.Cells["B1"].Value = "Name";
                worksheet.Cells["C1"].Value = "Type";
                worksheet.Cells["D1"].Value = "Description";

                var headerRange = worksheet.Cells["A1:D1"];
                FormatExcelHeaders(headerRange);

                int row = 2;

                // Add holidays
                foreach (var holiday in holidays)
                {
                    worksheet.Cells[row, 1].Value = holiday.Date.ToString("MM/dd/yyyy");
                    worksheet.Cells[row, 2].Value = holiday.Name;
                    worksheet.Cells[row, 3].Value = "Holiday";
                    worksheet.Cells[row, 4].Value = holiday.Description;
                    row++;
                }

                // Add events
                foreach (var evt in monthEvents)
                {
                    worksheet.Cells[row, 1].Value = evt.StartDate.ToString("MM/dd/yyyy");
                    worksheet.Cells[row, 2].Value = evt.Name;
                    worksheet.Cells[row, 3].Value = "Event";
                    worksheet.Cells[row, 4].Value = evt.Description;
                    row++;
                }

                worksheet.Cells[worksheet.Dimension.Address].AutoFitColumns();
                return package.GetAsByteArray();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error exporting month to Excel for calendar {CalendarId}", calendarId);
            throw;
        }
    }

    public async Task<byte[]> ExportMonthToPdfAsync(Guid calendarId, DateTime month)
    {
        try
        {
            var boldFont = GetBoldFont();
            var normalFont = GetNormalFont();
            var calendar = await GetCalendarByIdAsync(calendarId);
            if (calendar == null)
                throw new ArgumentException("Calendar not found");

            var startDate = new DateTime(month.Year, month.Month, 1);
            var endDate = startDate.AddMonths(1).AddDays(-1);

            var holidays = calendar.Holidays
                .Where(h => h.Date >= startDate && h.Date <= endDate)
                .OrderBy(h => h.Date);

            var events = await GetEventsByCalendarIdAsync(calendarId);
            var monthEvents = events
                .Where(e => e.StartDate <= endDate && e.EndDate >= startDate)
                .OrderBy(e => e.StartDate);

            using var memoryStream = new MemoryStream();
            using (var writer = new PdfWriter(memoryStream))
            using (var pdf = new PdfDocument(writer))
            using (var document = new Document(pdf))
            {
                // Add title
                document.Add(new Paragraph($"Calendar: {calendar.Calendar.Name}")
                    .SetFontSize(16)
                    .SetFont(boldFont)
                    .SetTextAlignment(TextAlignment.CENTER));

                document.Add(new Paragraph($"Events and Holidays - {month:MMMM yyyy}")
                    .SetFontSize(14)
                    .SetTextAlignment(TextAlignment.CENTER));

                // Create table
                var table = new Table(4).UseAllAvailableWidth();

                // Add headers
                string[] headers = { "Date", "Name", "Type", "Description" };
                foreach (var header in headers)
                {
                    table.AddHeaderCell(new Cell().Add(new Paragraph(header).SetFont(boldFont)));
                }

                // Add holidays
                foreach (var holiday in holidays)
                {
                    AddPdfRow(table, holiday.Date.ToString("MM/dd/yyyy"),
                        holiday.Name, "Holiday", holiday.Description);
                }

                // Add events
                foreach (var evt in monthEvents)
                {
                    AddPdfRow(table, evt.StartDate.ToString("MM/dd/yyyy"),
                        evt.Name, "Event", evt.Description);
                }

                document.Add(table);
            }

            return memoryStream.ToArray();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error exporting month to PDF for calendar {CalendarId}", calendarId);
            throw;
        }
    }

    public async Task<byte[]> ExportMonthToCsvAsync(Guid calendarId, DateTime month)
    {
        try
        {
            var calendar = await GetCalendarByIdAsync(calendarId);
            if (calendar == null)
                throw new ArgumentException("Calendar not found");

            var startDate = new DateTime(month.Year, month.Month, 1);
            var endDate = startDate.AddMonths(1).AddDays(-1);

            var allEvents = new List<(DateTime Date, string Name, string Type, string Description)>();

            // Add holidays
            allEvents.AddRange(calendar.Holidays
                .Where(h => h.Date >= startDate && h.Date <= endDate)
                .Select(h => (h.Date, h.Name, "Holiday", h.Description)));

            // Add events
            var events = await GetEventsByCalendarIdAsync(calendarId);
            allEvents.AddRange(events
                .Where(e => e.StartDate <= endDate && e.EndDate >= startDate)
                .Select(e => (e.StartDate, e.Name, "Event", e.Description)));

            using var memoryStream = new MemoryStream();
            using (var writer = new StreamWriter(memoryStream, Encoding.UTF8))
            {
                // Write header
                await writer.WriteLineAsync("Date,Name,Type,Description");

                // Write data
                foreach (var evt in allEvents.OrderBy(e => e.Date))
                {
                    await writer.WriteLineAsync($"{evt.Date:MM/dd/yyyy},{EscapeCsvField(evt.Name)},{evt.Type},{EscapeCsvField(evt.Description)}");
                }
            }

            return memoryStream.ToArray();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error exporting month to CSV for calendar {CalendarId}", calendarId);
            throw;
        }
    }

    // Helper methods
    private void FormatExcelHeaders(ExcelRange headerRange)
    {
        headerRange.Style.Font.Bold = true;
        headerRange.Style.Fill.PatternType = ExcelFillStyle.Solid;
        headerRange.Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.LightGray);
    }

    private void AddPdfRow(Table table, string date, string name, string type, string description)
    {
        table.AddCell(new Cell().Add(new Paragraph(date)));
        table.AddCell(new Cell().Add(new Paragraph(name)));
        table.AddCell(new Cell().Add(new Paragraph(type)));
        table.AddCell(new Cell().Add(new Paragraph(description ?? "")));
    }

    private string EscapeCsvField(string field)
    {
        if (string.IsNullOrEmpty(field))
            return "";

        return $"\"{field.Replace("\"", "\"\"")}\"";
    }

    public async Task<byte[]> ExportMonthToIcsAsync(Guid calendarId, DateTime month)
    {
        try
        {
            var calendar = await GetCalendarByIdAsync(calendarId);
            if (calendar == null)
                throw new ArgumentException("Calendar not found");

            var startDate = new DateTime(month.Year, month.Month, 1);
            var endDate = startDate.AddMonths(1).AddDays(-1);

            var holidays = calendar.Holidays
                .Where(h => h.Date >= startDate && h.Date <= endDate);

            var events = await GetEventsByCalendarIdAsync(calendarId);
            var monthEvents = events
                .Where(e => e.StartDate <= endDate && e.EndDate >= startDate);

            var sb = new StringBuilder();
            sb.AppendLine("BEGIN:VCALENDAR");
            sb.AppendLine("VERSION:2.0");
            sb.AppendLine($"PRODID:-//HolidayCalendar//{calendar.Calendar.Name}//EN");
            sb.AppendLine("CALSCALE:GREGORIAN");
            sb.AppendLine("METHOD:PUBLISH");

            // Add holidays
            foreach (var holiday in holidays)
            {
                AppendIcsEvent(sb, holiday.Id.ToString(), holiday.Name,
                    holiday.Date, holiday.Date.AddDays(1), "Holiday", holiday.Description);
            }

            // Add events
            foreach (var evt in monthEvents)
            {
                AppendIcsEvent(sb, evt.Id.ToString(), evt.Name,
                    evt.StartDate, evt.EndDate, "Event", evt.Description);
            }

            sb.AppendLine("END:VCALENDAR");
            return Encoding.UTF8.GetBytes(sb.ToString());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error exporting month to ICS for calendar {CalendarId}", calendarId);
            throw;
        }
    }

    public async Task<byte[]> ExportYearToExcelAsync(Guid calendarId, int year)
    {
        try
        {
            var calendar = await GetCalendarByIdAsync(calendarId);
            if (calendar == null)
                throw new ArgumentException("Calendar not found");

            var startDate = new DateTime(year, 1, 1);
            var endDate = new DateTime(year, 12, 31);

            var holidays = calendar.Holidays
                .Where(h => h.Date.Year == year)
                .OrderBy(h => h.Date);

            var events = await GetEventsByCalendarIdAsync(calendarId);
            var yearEvents = events
                .Where(e => e.StartDate.Year == year || e.EndDate.Year == year)
                .OrderBy(e => e.StartDate);

            using (var package = new ExcelPackage())
            {
                var worksheet = package.Workbook.Worksheets.Add($"Events {year}");

                // Headers
                worksheet.Cells["A1"].Value = "Date";
                worksheet.Cells["B1"].Value = "Name";
                worksheet.Cells["C1"].Value = "Type";
                worksheet.Cells["D1"].Value = "Description";
                worksheet.Cells["E1"].Value = "Month";

                var headerRange = worksheet.Cells["A1:E1"];
                FormatExcelHeaders(headerRange);

                int row = 2;

                // Add holidays and events for each month
                for (int month = 1; month <= 12; month++)
                {
                    var monthName = new DateTime(year, month, 1).ToString("MMMM");

                    // Add holidays for this month
                    var monthHolidays = holidays.Where(h => h.Date.Month == month);
                    foreach (var holiday in monthHolidays)
                    {
                        worksheet.Cells[row, 1].Value = holiday.Date.ToString("MM/dd/yyyy");
                        worksheet.Cells[row, 2].Value = holiday.Name;
                        worksheet.Cells[row, 3].Value = "Holiday";
                        worksheet.Cells[row, 4].Value = holiday.Description;
                        worksheet.Cells[row, 5].Value = monthName;
                        row++;
                    }

                    // Add events for this month
                    var monthEvents = yearEvents.Where(e => e.StartDate.Month == month);
                    foreach (var evt in monthEvents)
                    {
                        worksheet.Cells[row, 1].Value = evt.StartDate.ToString("MM/dd/yyyy");
                        worksheet.Cells[row, 2].Value = evt.Name;
                        worksheet.Cells[row, 3].Value = "Event";
                        worksheet.Cells[row, 4].Value = evt.Description;
                        worksheet.Cells[row, 5].Value = monthName;
                        row++;
                    }
                }

                worksheet.Cells[worksheet.Dimension.Address].AutoFitColumns();
                return package.GetAsByteArray();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error exporting year to Excel for calendar {CalendarId}", calendarId);
            throw;
        }
    }

    public async Task<byte[]> ExportYearToPdfAsync(Guid calendarId, int year)
    {
        try
        {
            var boldFont = GetBoldFont();
            var normalFont = GetNormalFont();
            var calendar = await GetCalendarByIdAsync(calendarId);
            if (calendar == null)
                throw new ArgumentException("Calendar not found");

            var holidays = calendar.Holidays
                .Where(h => h.Date.Year == year)
                .OrderBy(h => h.Date);

            var events = await GetEventsByCalendarIdAsync(calendarId);
            var yearEvents = events
                .Where(e => e.StartDate.Year == year || e.EndDate.Year == year)
                .OrderBy(e => e.StartDate);

            using var memoryStream = new MemoryStream();
            using (var writer = new PdfWriter(memoryStream))
            using (var pdf = new PdfDocument(writer))
            using (var document = new Document(pdf))
            {
                // Add title
                document.Add(new Paragraph($"Calendar: {calendar.Calendar.Name}")
                .SetFontSize(16)
                .SetFont(boldFont)
                .SetTextAlignment(TextAlignment.CENTER));

                document.Add(new Paragraph($"Annual Events and Holidays - {year}")
                    .SetFontSize(14)
                    .SetTextAlignment(TextAlignment.CENTER));

                // Create events for each month
                for (int month = 1; month <= 12; month++)
                {
                    var monthName = new DateTime(year, month, 1).ToString("MMMM");
                    document.Add(new Paragraph(monthName)
                        .SetFontSize(12)
                        .SetFont(boldFont));

                    var table = new Table(4).UseAllAvailableWidth();
                    string[] headers = { "Date", "Name", "Type", "Description" };
                    foreach (var header in headers)
                    {
                        table.AddHeaderCell(new Cell().Add(new Paragraph(header)
                        .SetFont(PdfFontFactory.CreateFont(StandardFonts.HELVETICA_BOLD))));
                    }

                    // Add holidays for this month
                    var monthHolidays = holidays.Where(h => h.Date.Month == month);
                    foreach (var holiday in monthHolidays)
                    {
                        AddPdfRow(table, holiday.Date.ToString("MM/dd/yyyy"),
                            holiday.Name, "Holiday", holiday.Description);
                    }

                    // Add events for this month
                    var monthEvents = yearEvents.Where(e => e.StartDate.Month == month);
                    foreach (var evt in monthEvents)
                    {
                        AddPdfRow(table, evt.StartDate.ToString("MM/dd/yyyy"),
                            evt.Name, "Event", evt.Description);
                    }

                    document.Add(table);
                    document.Add(new Paragraph("\n"));
                }
            }

            return memoryStream.ToArray();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error exporting year to PDF for calendar {CalendarId}", calendarId);
            throw;
        }
    }

    private PdfFont GetBoldFont()
    {
        return PdfFontFactory.CreateFont(StandardFonts.HELVETICA_BOLD);
    }

    private PdfFont GetNormalFont()
    {
        return PdfFontFactory.CreateFont(StandardFonts.HELVETICA);
    }

    public async Task<byte[]> ExportYearToCsvAsync(Guid calendarId, int year)
    {
        try
        {
            var calendar = await GetCalendarByIdAsync(calendarId);
            if (calendar == null)
                throw new ArgumentException("Calendar not found");

            var allEvents = new List<(DateTime Date, string Name, string Type, string Description, string Month)>();

            // Add holidays
            allEvents.AddRange(calendar.Holidays
                .Where(h => h.Date.Year == year)
                .Select(h => (h.Date, h.Name, "Holiday", h.Description, h.Date.ToString("MMMM"))));

            // Add events
            var events = await GetEventsByCalendarIdAsync(calendarId);
            allEvents.AddRange(events
                .Where(e => e.StartDate.Year == year || e.EndDate.Year == year)
                .Select(e => (e.StartDate, e.Name, "Event", e.Description, e.StartDate.ToString("MMMM"))));

            using var memoryStream = new MemoryStream();
            using (var writer = new StreamWriter(memoryStream, Encoding.UTF8))
            {
                // Write header
                await writer.WriteLineAsync("Date,Name,Type,Description,Month");

                // Write data
                foreach (var evt in allEvents.OrderBy(e => e.Date))
                {
                    await writer.WriteLineAsync(
                        $"{evt.Date:MM/dd/yyyy}," +
                        $"{EscapeCsvField(evt.Name)}," +
                        $"{evt.Type}," +
                        $"{EscapeCsvField(evt.Description)}," +
                        $"{evt.Month}");
                }
            }

            return memoryStream.ToArray();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error exporting year to CSV for calendar {CalendarId}", calendarId);
            throw;
        }
    }

    public async Task<byte[]> ExportYearToIcsAsync(Guid calendarId, int year)
    {
        try
        {
            var calendar = await GetCalendarByIdAsync(calendarId);
            if (calendar == null)
                throw new ArgumentException("Calendar not found");

            var holidays = calendar.Holidays
                .Where(h => h.Date.Year == year);

            var events = await GetEventsByCalendarIdAsync(calendarId);
            var yearEvents = events
                .Where(e => e.StartDate.Year == year || e.EndDate.Year == year);

            var sb = new StringBuilder();
            sb.AppendLine("BEGIN:VCALENDAR");
            sb.AppendLine("VERSION:2.0");
            sb.AppendLine($"PRODID:-//HolidayCalendar//{calendar.Calendar.Name}//EN");
            sb.AppendLine("CALSCALE:GREGORIAN");
            sb.AppendLine("METHOD:PUBLISH");

            // Add holidays
            foreach (var holiday in holidays)
            {
                AppendIcsEvent(sb, holiday.Id.ToString(), holiday.Name,
                    holiday.Date, holiday.Date.AddDays(1), "Holiday", holiday.Description);
            }

            // Add events
            foreach (var evt in yearEvents)
            {
                AppendIcsEvent(sb, evt.Id.ToString(), evt.Name,
                    evt.StartDate, evt.EndDate, "Event", evt.Description);
            }

            sb.AppendLine("END:VCALENDAR");
            return Encoding.UTF8.GetBytes(sb.ToString());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error exporting year to ICS for calendar {CalendarId}", calendarId);
            throw;
        }
    }

    private void AppendIcsEvent(StringBuilder sb, string uid, string summary,
        DateTime start, DateTime end, string type, string description)
    {
        sb.AppendLine("BEGIN:VEVENT");
        sb.AppendLine($"UID:{uid}");
        sb.AppendLine($"DTSTAMP:{DateTime.UtcNow:yyyyMMddTHHmmssZ}");
        sb.AppendLine($"DTSTART:{start:yyyyMMddTHHmmss}");
        sb.AppendLine($"DTEND:{end:yyyyMMddTHHmmss}");
        sb.AppendLine($"SUMMARY:{EscapeIcsField(summary)}");
        sb.AppendLine($"CATEGORIES:{type}");
        if (!string.IsNullOrEmpty(description))
        {
            sb.AppendLine($"DESCRIPTION:{EscapeIcsField(description)}");
        }
        sb.AppendLine("END:VEVENT");
    }

    private string EscapeIcsField(string field)
    {
        if (string.IsNullOrEmpty(field))
            return "";

        return field.Replace("\\", "\\\\")
                   .Replace(";", "\\;")
                   .Replace(",", "\\,")
                   .Replace("\n", "\\n")
                   .Replace("\r", "");
    }



}
