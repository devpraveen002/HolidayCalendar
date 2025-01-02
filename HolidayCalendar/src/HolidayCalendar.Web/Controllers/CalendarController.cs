using HolidayCalendar.src.HolidayCalendar.Core.Entities;
using HolidayCalendar.src.HolidayCalendar.Core.Services;
using HolidayCalendar.src.HolidayCalendar.Web.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OfficeOpenXml;
using OfficeOpenXml.Style;
using System.ComponentModel;
using System.Diagnostics;
using System.Security.Claims;
using System.Text;
using LicenseContext = OfficeOpenXml.LicenseContext;

namespace HolidayCalendar.src.HolidayCalendar.Web.Controllers;

public class CalendarController : Controller
{
    private readonly ICalendarService _calendarService;
    private readonly ILogger<CalendarController> _logger;

    public CalendarController(
        ICalendarService calendarService,
        ILogger<CalendarController> logger)
    {
        _calendarService = calendarService;
        _logger = logger;
    }

    public async Task<IActionResult> Index(string shareableLink = null, int? month = null, int? year = null)
    {
        try
        {
            // Default to current month and year if not specified
            var currentDate = DateTime.Now;
            var currentMonth = month ?? currentDate.Month;
            var currentYear = year ?? currentDate.Year;

            // Calculate previous and next months
            var previousDate = new DateTime(currentYear, currentMonth, 1).AddMonths(-1);
            var nextDate = new DateTime(currentYear, currentMonth, 1).AddMonths(1);

            Calendar calendar;
            if (!string.IsNullOrEmpty(shareableLink))
            {
                calendar = await _calendarService.GetByShareableLinkAsync(shareableLink);
            }
            else
            {
                calendar = await _calendarService.GetDefaultCalendarAsync();
            }

            if (calendar == null)
            {
                TempData["ErrorMessage"] = "Calendar not found. Please ensure the database is properly seeded.";
                return RedirectToAction("Error", "Home");
            }

            var viewModel = new CalendarViewModel
            {
                Calendar = calendar,
                IsEditable = User.Identity.IsAuthenticated &&
                            calendar.UserId == User.FindFirstValue(ClaimTypes.NameIdentifier),
                CurrentMonth = currentMonth,
                CurrentYear = currentYear,
                PreviousMonth = previousDate.Month,
                PreviousYear = previousDate.Year,
                NextMonth = nextDate.Month,
                NextYear = nextDate.Year,
                ShareableLink = shareableLink
            };

            return View(viewModel);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving calendar");
            TempData["ErrorMessage"] = "An error occurred while retrieving the calendar. Please try again later.";
            return RedirectToAction("Error", "Home");
        }
    }

    [HttpPost]
    [Authorize]
    public async Task<IActionResult> AddHoliday(AddHolidayViewModel model)
    {
        try
        {
            if (!ModelState.IsValid)
                return View(model);

            var holiday = new Holiday
            {
                Name = model.Name,
                Date = model.Date,
                IsFixedHoliday = model.IsFixedHoliday,
                IsWeekendAdjustable = model.IsWeekendAdjustable
            };

            await _calendarService.AddHolidayAsync(model.CalendarId, holiday);
            return RedirectToAction(nameof(Index));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding holiday");
            return RedirectToAction("Error", "Home");
        }
    }

    [Authorize]
    [HttpGet]
    public IActionResult Create()
    {
        return View(new CreateCalendarViewModel());
    }

    [Authorize]
    [HttpPost]
    public async Task<IActionResult> Create(CreateCalendarViewModel model)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                return RedirectToAction("Login", "Account");
            }

            var newCalendar = await _calendarService.CreateUserCalendarAsync(userId, model.Name);
            if (newCalendar == null)
            {
                TempData["Error"] = "Unable to create calendar. Please try again.";
                return RedirectToAction(nameof(Dashboard));
            }

            TempData["Success"] = "Calendar created successfully!";
            return RedirectToAction(nameof(Dashboard));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating calendar");
            TempData["Error"] = "An error occurred while creating the calendar.";
            return RedirectToAction("Error", "Home");
        }
    }

    [Authorize]
    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        try
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var calendar = await _calendarService.GetCalendarByIdAsync(id);

            if (calendar == null || calendar.UserId != userId)
            {
                return NotFound();
            }

            var viewModel = new CalendarViewModel
            {
                Calendar = calendar
            };

            return View(viewModel);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading calendar for edit");
            TempData["Error"] = "An error occurred while loading the calendar.";
            return RedirectToAction(nameof(Dashboard));
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, string name)
    {
        try
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var calendar = await _calendarService.GetCalendarByIdAsync(id);

            if (calendar == null || calendar.UserId != userId)
                return NotFound();

            calendar.Name = name;
            await _calendarService.UpdateCalendarAsync(calendar);
            TempData["Success"] = "Calendar updated successfully!";
            return RedirectToAction(nameof(Dashboard));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating calendar");
            TempData["Error"] = "An error occurred while updating the calendar.";
            return RedirectToAction(nameof(Dashboard));
        }
    }

    [Authorize]
    [HttpPost]
    public async Task<IActionResult> Delete(int id)
    {
        try
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var calendar = await _calendarService.GetCalendarByIdAsync(id);

            if (calendar == null || calendar.UserId != userId)
            {
                return NotFound();
            }

            if (calendar.IsDefault)
            {
                TempData["Error"] = "Cannot delete the default calendar.";
                return RedirectToAction(nameof(Dashboard));
            }

            await _calendarService.DeleteCalendarAsync(id);
            TempData["Success"] = "Calendar deleted successfully!";
            return RedirectToAction(nameof(Dashboard));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting calendar");
            TempData["Error"] = "An error occurred while deleting the calendar.";
            return RedirectToAction(nameof(Dashboard));
        }
    }

    [Authorize]
    public async Task<IActionResult> Dashboard()
    {
        try
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                return RedirectToAction("Login", "Account");
            }

            var calendars = await _calendarService.GetUserCalendarsAsync(userId);
            var viewModel = new DashboardViewModel
            {
                Calendars = calendars ?? new List<Calendar>()
            };

            return View(viewModel);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading dashboard for user {UserId}: {Error}",
                User.FindFirstValue(ClaimTypes.NameIdentifier), ex.Message);
            TempData["Error"] = "An error occurred while loading your calendars.";
            return RedirectToAction("Error", "Home");
        }
    }

    public async Task<IActionResult> View(int id, int? month = null, int? year = null)
    {
        var calendar = await _calendarService.GetCalendarByIdAsync(id);
        if (calendar == null) return NotFound();

        var currentDate = DateTime.Now;
        var viewModel = new CalendarViewModel
        {
            Calendar = calendar,
            CurrentMonth = month ?? currentDate.Month,
            CurrentYear = year ?? currentDate.Year,
            IsEditable = true
        };

        var currentMonth = new DateTime(viewModel.CurrentYear, viewModel.CurrentMonth, 1);
        viewModel.PreviousMonth = currentMonth.AddMonths(-1).Month;
        viewModel.PreviousYear = currentMonth.AddMonths(-1).Year;
        viewModel.NextMonth = currentMonth.AddMonths(1).Month;
        viewModel.NextYear = currentMonth.AddMonths(1).Year;

        return View(viewModel);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddHolidayEvent(int calendarId, string name, DateTime date, bool isFixedHoliday, bool isWeekendAdjustable)
    {
        try
        {
            var holiday = new Holiday
            {
                Name = name,
                Date = date.ToUniversalTime(),
                IsFixedHoliday = isFixedHoliday,
                IsWeekendAdjustable = isWeekendAdjustable,
                CalendarId = calendarId
            };

            await _calendarService.AddHolidayAsync(calendarId, holiday);
            TempData["Success"] = "Event added successfully";
            return RedirectToAction("View", new { id = calendarId });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding holiday");
            TempData["Error"] = "Failed to add event";
            return RedirectToAction("View", new { id = calendarId });
        }
    }

    public async Task<IActionResult> ExportToExcel(DateTime date)
    {
        try
        {
            var firstDayOfMonth = new DateTime(date.Year, date.Month, 1, 0, 0, 0, DateTimeKind.Utc);
            var lastDayOfMonth = firstDayOfMonth.AddMonths(1).AddDays(-1);

            var calendar = await _calendarService.GetDefaultCalendarAsync();
            var events = calendar.Holidays
                .Where(e => e.Date >= firstDayOfMonth && e.Date <= lastDayOfMonth)
                .OrderBy(e => e.Date)
                .ToList();

            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

            using (var package = new ExcelPackage())
            {
                var worksheet = package.Workbook.Worksheets.Add("Events");

                // Configure headers with styling
                ConfigureExcelHeaders(worksheet);

                // Add data rows
                AddExcelData(worksheet, events);

                // Auto-fit and format
                worksheet.Cells[worksheet.Dimension.Address].AutoFitColumns();

                return File(
                    package.GetAsByteArray(),
                    "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                    $"Calendar_Events_{date:MMMM_yyyy}.xlsx"
                );
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error exporting to Excel for date {Date}", date);
            TempData["Error"] = "Failed to export to Excel. Please try again.";
            return RedirectToAction(nameof(Index), new { date });
        }
    }

    public async Task<IActionResult> ExportToCsv(DateTime date)
    {
        try
        {
            var calendar = await _calendarService.GetDefaultCalendarAsync();
            var events = GetEventsForPeriod(calendar, date, true);

            using var memoryStream = new MemoryStream();
            using var writer = new StreamWriter(memoryStream);

            // Add UTF-8 BOM for Excel compatibility
            WriteUtf8Bom(memoryStream);

            // Write CSV content
            await WriteCsvContent(writer, events);

            return File(
                memoryStream.ToArray(),
                "application/vnd.ms-excel; charset=utf-8",
                $"Calendar_Events_{date:MMMM_yyyy}.csv"
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error exporting to CSV for date {Date}", date);
            TempData["Error"] = "Failed to export to CSV. Please try again.";
            return RedirectToAction(nameof(Index), new { date });
        }
    }

    public async Task<IActionResult> ExportToIcs(DateTime date)
    {
        try
        {
            var calendar = await _calendarService.GetDefaultCalendarAsync();
            var events = GetEventsForPeriod(calendar, date, true);

            var sb = new StringBuilder();
            WriteIcsHeader(sb);
            WriteIcsEvents(sb, events);
            sb.AppendLine("END:VCALENDAR");

            var bytes = Encoding.UTF8.GetBytes(sb.ToString());
            return File(bytes, "text/calendar", $"Calendar_Events_{date:MMMM_yyyy}.ics");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error exporting to ICS");
            TempData["Error"] = "Failed to export to ICS. Please try again.";
            return RedirectToAction(nameof(Index), new { date });
        }
    }

    #region Helper Methods
    private IEnumerable<Holiday> GetEventsForPeriod(Calendar calendar, DateTime date, bool isMonthly)
    {
        var firstDay = isMonthly
            ? new DateTime(date.Year, date.Month, 1, 0, 0, 0, DateTimeKind.Utc)
            : new DateTime(date.Year, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        var lastDay = isMonthly
            ? firstDay.AddMonths(1).AddDays(-1)
            : new DateTime(date.Year, 12, 31, 23, 59, 59, DateTimeKind.Utc);

        return calendar.Holidays
            .Where(e => e.Date >= firstDay && e.Date <= lastDay)
            .OrderBy(e => e.Date);
    }

    private void WriteIcsHeader(StringBuilder sb)
    {
        sb.AppendLine("BEGIN:VCALENDAR");
        sb.AppendLine("VERSION:2.0");
        sb.AppendLine("PRODID:-//YourCompany//Calendar App//EN");
        sb.AppendLine("CALSCALE:GREGORIAN");
        sb.AppendLine("METHOD:PUBLISH");
    }

    private void WriteIcsEvents(StringBuilder sb, IEnumerable<Holiday> events)
    {
        foreach (var evt in events)
        {
            sb.AppendLine("BEGIN:VEVENT");
            sb.AppendLine($"UID:{evt.Id}");
            sb.AppendLine($"DTSTAMP:{DateTime.UtcNow:yyyyMMddTHHmmssZ}");
            sb.AppendLine($"DTSTART;VALUE=DATE:{evt.Date:yyyyMMdd}");
            sb.AppendLine($"DTEND;VALUE=DATE:{evt.Date.AddDays(1):yyyyMMdd}");
            sb.AppendLine($"SUMMARY:{EscapeIcsField(evt.Name)}");
            sb.AppendLine("END:VEVENT");
        }
    }

    private string EscapeIcsField(string field)
    {
        if (string.IsNullOrEmpty(field)) return "";
        return field
            .Replace("\\", "\\\\")
            .Replace(";", "\\;")
            .Replace(",", "\\,")
            .Replace("\n", "\\n")
            .Replace("\r", "");
    }

    private string EscapeCsvField(string field)
    {
        if (string.IsNullOrEmpty(field)) return "\"\"";
        return $"\"{field.Replace("\"", "\"\"")}\"";
    }

    private void ConfigureExcelHeaders(ExcelWorksheet worksheet)
    {
        worksheet.Cells["A1"].Value = "Date";
        worksheet.Cells["B1"].Value = "Title";
        worksheet.Cells["C1"].Value = "Description";

        var headerRange = worksheet.Cells["A1:C1"];
        headerRange.Style.Font.Bold = true;
        headerRange.Style.Fill.PatternType = ExcelFillStyle.Solid;
        headerRange.Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.LightGray);
    }

    private void AddExcelData(ExcelWorksheet worksheet, IEnumerable<Holiday> events)
    {
        int row = 2;
        foreach (var evt in events)
        {
            worksheet.Cells[row, 1].Value = evt.Date.ToLocalTime().ToString("MM/dd/yyyy");
            worksheet.Cells[row, 2].Value = evt.Name;
            worksheet.Cells[row, 3].Value = evt.Description;
            row++;
        }
    }

    private void WriteUtf8Bom(Stream stream)
    {
        byte[] bom = new byte[] { 0xEF, 0xBB, 0xBF };
        stream.Write(bom, 0, bom.Length);
    }

    private async Task WriteCsvContent(StreamWriter writer, IEnumerable<Holiday> events)
    {
        writer.WriteLine("Date,Title,Description");
        foreach (var evt in events)
        {
            var dateStr = $"\"{evt.Date:yyyy-MM-dd}\"";
            var title = EscapeCsvField(evt.Name);
            var description = EscapeCsvField(evt.Description ?? "");
            await writer.WriteLineAsync($"{dateStr},{title},{description}");
        }
        await writer.FlushAsync();
    }
    #endregion

    public async Task<IActionResult> ExportYearToExcel(int year)
    {
        try
        {
            var calendar = await _calendarService.GetDefaultCalendarAsync();
            var events = GetEventsForPeriod(calendar, new DateTime(year, 1, 1), false);

            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

            using (var package = new ExcelPackage())
            {
                var worksheet = package.Workbook.Worksheets.Add($"Events {year}");
                ConfigureExcelHeaders(worksheet);
                AddExcelData(worksheet, events);
                worksheet.Cells[worksheet.Dimension.Address].AutoFitColumns();

                return File(
                    package.GetAsByteArray(),
                    "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                    $"Calendar_Events_{year}.xlsx"
                );
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error exporting year to Excel");
            TempData["Error"] = "Failed to export year to Excel. Please try again.";
            return RedirectToAction(nameof(Index), new { year });
        }
    }

    public async Task<IActionResult> ExportYearToCsv(int year)
    {
        try
        {
            var calendar = await _calendarService.GetDefaultCalendarAsync();
            var events = GetEventsForPeriod(calendar, new DateTime(year, 1, 1), false);

            using var memoryStream = new MemoryStream();
            using var writer = new StreamWriter(memoryStream);

            WriteUtf8Bom(memoryStream);
            await WriteCsvContent(writer, events);

            return File(
                memoryStream.ToArray(),
                "application/vnd.ms-excel; charset=utf-8",
                $"Calendar_Events_{year}.csv"
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error exporting year to CSV");
            TempData["Error"] = "Failed to export year to CSV. Please try again.";
            return RedirectToAction(nameof(Index), new { year });
        }
    }

    public async Task<IActionResult> ExportYearToIcs(int year)
    {
        try
        {
            var calendar = await _calendarService.GetDefaultCalendarAsync();
            var events = GetEventsForPeriod(calendar, new DateTime(year, 1, 1), false);

            var sb = new StringBuilder();
            WriteIcsHeader(sb);
            WriteIcsEvents(sb, events);
            sb.AppendLine("END:VCALENDAR");

            var bytes = Encoding.UTF8.GetBytes(sb.ToString());
            return File(bytes, "text/calendar", $"Calendar_Events_{year}.ics");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error exporting year to ICS");
            TempData["Error"] = "Failed to export year to ICS. Please try again.";
            return RedirectToAction(nameof(Index), new { year });
        }
    }

    [AllowAnonymous]
    public async Task<IActionResult> Share(string shareableLink, int? month = null, int? year = null)
    {
        var calendar = await _calendarService.GetByShareableLinkAsync(shareableLink);
        if (calendar == null) return NotFound();

        var currentDate = DateTime.Now;
        var viewModel = new CalendarViewModel
        {
            Calendar = calendar,
            CurrentMonth = month ?? currentDate.Month,
            CurrentYear = year ?? currentDate.Year,
            IsEditable = false,
            ShareableLink = shareableLink
        };

        var currentMonth = new DateTime(viewModel.CurrentYear, viewModel.CurrentMonth, 1);
        viewModel.PreviousMonth = currentMonth.AddMonths(-1).Month;
        viewModel.PreviousYear = currentMonth.AddMonths(-1).Year;
        viewModel.NextMonth = currentMonth.AddMonths(1).Month;
        viewModel.NextYear = currentMonth.AddMonths(1).Year;

        return View("View", viewModel);
    }

    public async Task<IActionResult> GenerateShareableLink(int calendarId)
    {
        try
        {
            var calendar = await _calendarService.GetCalendarByIdAsync(calendarId);
            if (calendar == null || calendar.UserId != User.FindFirstValue(ClaimTypes.NameIdentifier))
                return NotFound();

            var shareableLink = Guid.NewGuid().ToString();
            calendar.ShareableLink = shareableLink;
            await _calendarService.UpdateCalendarAsync(calendar);

            TempData["ShareableLink"] = $"{Request.Scheme}://{Request.Host}/Calendar/Share?shareableLink={shareableLink}";
            TempData["Success"] = "Shareable link generated successfully!";
            return RedirectToAction(nameof(Dashboard));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating shareable link");
            TempData["Error"] = "Failed to generate shareable link.";
            return RedirectToAction(nameof(Dashboard));
        }
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
