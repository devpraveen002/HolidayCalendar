using HolidayCalendar.src.HolidayCalendar.Core.Entities;
using HolidayCalendar.src.HolidayCalendar.Core.Services;
using HolidayCalendar.src.HolidayCalendar.Web.ViewModels;
using iText.Kernel.Pdf;
using iText.Layout;
using iText.Layout.Element;
using iText.Layout.Properties;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using Npgsql.Internal;
using OfficeOpenXml;
using OfficeOpenXml.Style;
using System.ComponentModel;
using System.Diagnostics;
using System.Reflection.Metadata;
using System.Security.Claims;
using System.Text;
using LicenseContext = OfficeOpenXml.LicenseContext;
using ITextDocument = iText.Layout.Document;
using ITextTable = iText.Layout.Element.Table;
using ITextParagraph = iText.Layout.Element.Paragraph;
using iText.IO.Font.Constants;
using iText.Kernel.Font;
using HolidayCalendar.src.HolidayCalendar.Core.DTOs;

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
            var currentDate = DateTime.Now;
            var currentMonth = month ?? currentDate.Month;
            var currentYear = year ?? currentDate.Year;

            CalendarDto calendarDto;
            if (!string.IsNullOrEmpty(shareableLink))
            {
                calendarDto = await _calendarService.GetByShareableLinkAsync(shareableLink);
            }
            else
            {
                calendarDto = await _calendarService.GetDefaultCalendarAsync();
            }

            if (calendarDto == null)
            {
                TempData["ErrorMessage"] = "Calendar not found. Please ensure the database is properly seeded.";
                return RedirectToAction("Error", "Home");
            }

            var viewModel = CalendarViewModel.FromDto(calendarDto,
                User.Identity.IsAuthenticated && calendarDto.Calendar.CreatedBy.ToString() == User.FindFirstValue(ClaimTypes.NameIdentifier));

            viewModel.CurrentMonth = currentMonth;
            viewModel.CurrentYear = currentYear;
            viewModel.ShareableLink = shareableLink;

            return View(viewModel);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving calendar");
            TempData["ErrorMessage"] = "An error occurred while retrieving the calendar.";
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
                Id = Guid.NewGuid(),
                Name = model.Name,
                Date = model.Date,
                IsFixedHoliday = model.IsFixedHoliday,
                IsWeekendAdjustable = model.IsWeekendAdjustable,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = long.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier))
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
    public async Task<IActionResult> Edit(Guid id)
    {
        try
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var calendarDto = await _calendarService.GetCalendarByIdAsync(id);

            if (calendarDto == null || calendarDto.Calendar.CreatedBy.ToString() != userId)
            {
                return NotFound();
            }

            var viewModel = CalendarViewModel.FromDto(calendarDto, true);
            return View(viewModel);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading calendar for edit");
            TempData["Error"] = "An error occurred while loading the calendar.";
            return RedirectToAction(nameof(Dashboard));
        }
    }

    [Authorize]
    [HttpPost]
    public async Task<IActionResult> Delete(Guid id)
    {
        try
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var calendarDto = await _calendarService.GetCalendarByIdAsync(id);

            if (calendarDto == null || calendarDto.Calendar.CreatedBy.ToString() != userId)
            {
                return NotFound();
            }

            if (calendarDto.Calendar.IsDefault)
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
                Calendars = calendars.Select(c => new CalendarSummaryViewModel
                {
                    Id = c.Calendar.Id,
                    Name = c.Calendar.Name,
                    IsDefault = c.Calendar.IsDefault,
                    ShareableLink = c.ShareableLink,
                    HolidayCount = c.Holidays?.Count ?? 0
                }).ToList()
            };

            return View(viewModel);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading dashboard for user {UserId}", User.FindFirstValue(ClaimTypes.NameIdentifier));
            TempData["Error"] = "An error occurred while loading your calendars.";
            return RedirectToAction("Error", "Home");
        }
    }

    public async Task<IActionResult> View(Guid id, int? month = null, int? year = null)
    {
        var calendarDto = await _calendarService.GetCalendarByIdAsync(id);
        if (calendarDto == null) return NotFound();

        var currentDate = DateTime.Now;
        var viewModel = CalendarViewModel.FromDto(calendarDto, true);

        viewModel.CurrentMonth = month ?? currentDate.Month;
        viewModel.CurrentYear = year ?? currentDate.Year;

        var currentMonth = new DateTime(viewModel.CurrentYear, viewModel.CurrentMonth, 1);
        viewModel.PreviousMonth = currentMonth.AddMonths(-1).Month;
        viewModel.PreviousYear = currentMonth.AddMonths(-1).Year;
        viewModel.NextMonth = currentMonth.AddMonths(1).Month;
        viewModel.NextYear = currentMonth.AddMonths(1).Year;

        return View(viewModel);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddHolidayEvent(Guid calendarId, string name, DateTime date, bool isFixedHoliday, bool isWeekendAdjustable)
    {
        try
        {
            var holiday = new Holiday
            {
                Id = Guid.NewGuid(),
                Name = name,
                Date = date.ToUniversalTime(),
                IsFixedHoliday = isFixedHoliday,
                IsWeekendAdjustable = isWeekendAdjustable,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = long.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier))
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

            var calendarDto = await _calendarService.GetDefaultCalendarAsync();
            var events = calendarDto.Holidays
                .Where(e => e.Date >= firstDayOfMonth && e.Date <= lastDayOfMonth)
                .OrderBy(e => e.Date)
                .ToList();

            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

            using (var package = new ExcelPackage())
            {
                var worksheet = package.Workbook.Worksheets.Add("Events");
                ConfigureExcelHeaders(worksheet);
                AddExcelData(worksheet, events);
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

    [HttpGet]
    public async Task<IActionResult> ExportToPdf(DateTime date)
    {
        try
        {
            var calendarDto = await _calendarService.GetDefaultCalendarAsync();
            if (calendarDto == null) return NotFound();

            var events = calendarDto.Holidays
                .Where(e => e.Date.Month == date.Month && e.Date.Year == date.Year)
                .OrderBy(e => e.Date)
                .ToList();

            byte[] pdfBytes;
            using (var memoryStream = new MemoryStream())
            {
                var writer = new PdfWriter(memoryStream);
                var pdf = new PdfDocument(writer);
                var document = new ITextDocument(pdf);

                AddPdfHeader(document, $"Calendar Events - {date:MMMM yyyy}");
                AddPdfContent(document, events);

                document.Close();
                pdf.Close();
                writer.Close();

                pdfBytes = memoryStream.ToArray();
            }

            return new FileContentResult(pdfBytes, "application/pdf")
            {
                FileDownloadName = $"Calendar_Events_{date:MMMM_yyyy}.pdf"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "PDF export failed");
            return BadRequest();
        }
    }

    [HttpGet]
    public async Task<IActionResult> ExportYearToPdf(int year)
    {
        try
        {
            var calendar = await _calendarService.GetDefaultCalendarAsync();
            if (calendar == null) return NotFound();

            var events = calendar.Holidays
                .Where(e => e.Date.Year == year)
                .OrderBy(e => e.Date)
                .ToList();

            byte[] pdfBytes;
            using (var memoryStream = new MemoryStream())
            {
                var writer = new PdfWriter(memoryStream);
                var pdf = new PdfDocument(writer);
                var document = new ITextDocument(pdf);

                AddPdfHeader(document, $"Calendar Events - Year {year}");
                AddPdfContent(document, events);

                document.Close();
                pdf.Close();
                writer.Close();

                pdfBytes = memoryStream.ToArray();
            }

            return new FileContentResult(pdfBytes, "application/pdf")
            {
                FileDownloadName = $"Calendar_Events_{year}.pdf"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "PDF export failed for year {Year}", year);
            return BadRequest();
        }
    }

    private void AddPdfHeader(ITextDocument document, string title)
    {
        var titleFont = PdfFontFactory.CreateFont(StandardFonts.HELVETICA_BOLD);
        document.Add(new ITextParagraph(title)
            .SetFontSize(20)
            .SetFont(titleFont)
            .SetTextAlignment(TextAlignment.CENTER));
    }

    private void AddPdfContent(ITextDocument document, List<Holiday> events)
    {
        if (!events.Any())
        {
            document.Add(new ITextParagraph("No events found.")
                .SetTextAlignment(TextAlignment.CENTER));
            return;
        }

        var table = new ITextTable(3).UseAllAvailableWidth();
        var headerFont = PdfFontFactory.CreateFont(StandardFonts.HELVETICA_BOLD);
        var normalFont = PdfFontFactory.CreateFont(StandardFonts.HELVETICA);

        // Add headers
        string[] headers = { "Date", "Event", "Description" };
        foreach (var header in headers)
        {
            table.AddHeaderCell(new Cell().Add(new ITextParagraph(header).SetFont(headerFont)));
        }

        // Add data
        foreach (var evt in events)
        {
            table.AddCell(new Cell().Add(new ITextParagraph(evt.Date.ToString("d")).SetFont(normalFont)));
            table.AddCell(new Cell().Add(new ITextParagraph(evt.Name).SetFont(normalFont)));
            table.AddCell(new Cell().Add(new ITextParagraph(evt.Description ?? "").SetFont(normalFont)));
        }

        document.Add(table);
    }

    public async Task<IActionResult> ExportToCsv(DateTime date)
    {
        try
        {
            var calendarDto = await _calendarService.GetDefaultCalendarAsync();
            var events = GetEventsForPeriod(calendarDto, date, true);

            using var memoryStream = new MemoryStream();
            using var writer = new StreamWriter(memoryStream);

            WriteUtf8Bom(memoryStream);
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
    private IEnumerable<Holiday> GetEventsForPeriod(CalendarDto calendarDto, DateTime date, bool isMonthly)
    {
        var firstDay = isMonthly
            ? new DateTime(date.Year, date.Month, 1, 0, 0, 0, DateTimeKind.Utc)
            : new DateTime(date.Year, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        var lastDay = isMonthly
            ? firstDay.AddMonths(1).AddDays(-1)
            : new DateTime(date.Year, 12, 31, 23, 59, 59, DateTimeKind.Utc);

        return calendarDto.Holidays
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
        var calendarDto = await _calendarService.GetByShareableLinkAsync(shareableLink);
        if (calendarDto == null) return NotFound();

        var currentDate = DateTime.Now;
        var viewModel = CalendarViewModel.FromDto(calendarDto, false);

        viewModel.CurrentMonth = month ?? currentDate.Month;
        viewModel.CurrentYear = year ?? currentDate.Year;
        viewModel.ShareableLink = shareableLink;

        var currentMonth = new DateTime(viewModel.CurrentYear, viewModel.CurrentMonth, 1);
        viewModel.PreviousMonth = currentMonth.AddMonths(-1).Month;
        viewModel.PreviousYear = currentMonth.AddMonths(-1).Year;
        viewModel.NextMonth = currentMonth.AddMonths(1).Month;
        viewModel.NextYear = currentMonth.AddMonths(1).Year;

        return View("View", viewModel);
    }

    public async Task<IActionResult> GenerateShareableLink(Guid calendarId)
    {
        try
        {
            var calendarDto = await _calendarService.GetCalendarByIdAsync(calendarId);
            if (calendarDto == null || calendarDto.Calendar.CreatedBy.ToString() != User.FindFirstValue(ClaimTypes.NameIdentifier))
                return NotFound();

            var shareableLink = Guid.NewGuid().ToString();
            calendarDto.Calendar.ShareableLink = shareableLink;
            await _calendarService.UpdateCalendarAsync(calendarDto.Calendar);

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
