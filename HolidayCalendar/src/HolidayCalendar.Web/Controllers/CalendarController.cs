using HolidayCalendar.src.HolidayCalendar.Core.Entities;
using HolidayCalendar.src.HolidayCalendar.Core.Services;
using HolidayCalendar.src.HolidayCalendar.Web.ViewModels;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using System.Security.Claims;

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

    [AllowAnonymous]
    public async Task<IActionResult> Index(string countryCode = "US", int? month = null, int? year = null)
    {
        try
        {
            var currentDate = DateTime.Now;
            var currentMonth = month ?? currentDate.Month;
            var currentYear = year ?? currentDate.Year;

            // Fetch calendar and countries concurrently
            var calendarTask = _calendarService.GetDefaultCalendarByCountryAsync(countryCode);
            var countriesTask = _calendarService.GetDefaultCalendarCountriesAsync();

            var calendarDto = await calendarTask;

            if (calendarDto == null)
            {
                _logger.LogWarning("No calendar found for country code: {CountryCode}", countryCode);
                TempData["Error"] = "Calendar not found for the selected country.";
                return RedirectToAction("Error", "Home");
            }

            var countries = await countriesTask;

            // Create view model
            var viewModel = new CalendarViewModel
            {
                Calendar = calendarDto.Calendar,
                Holidays = calendarDto.Holidays ?? new List<Holiday>(),
                Events = await _calendarService.GetEventsByCalendarIdAsync(calendarDto.Calendar.Id),
                CurrentMonth = currentMonth,
                CurrentYear = currentYear,
                SelectedCountry = countryCode,
                IsEditable = User.Identity.IsAuthenticated && User.IsInRole("Admin"),
                ShareableLink = calendarDto.ShareableLink,
                AvailableCountries = countries?.Select(c => new SelectListItem
                {
                    Value = c.CountryCode,
                    Text = c.CountryName,
                    Selected = c.CountryCode == countryCode
                }).ToList() ?? new List<SelectListItem>()
            };

            // Set navigation dates
            viewModel.UpdateNavigationDates();

            return View(viewModel);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading calendar for country code: {CountryCode}", countryCode);
            TempData["Error"] = "An error occurred while loading the calendar.";
            return RedirectToAction("Error", "Home");
        }
    }


    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> AdminDashboard()
    {
        var calendars = await _calendarService.GetAllCalendarsAsync();
        return View(calendars);
    }

    [Authorize(Roles = "Admin")]
    [HttpPost]
    public async Task<IActionResult> CreateCountryCalendar(CreateCalendarViewModel model)
    {
        if (ModelState.IsValid)
        {
            await _calendarService.CreateDefaultCalendarAsync(model);
            return RedirectToAction(nameof(AdminDashboard));
        }
        return View(model);
    }

    [Authorize(Roles = "Admin")]
    [HttpPost]
    public async Task<IActionResult> EditEvent(EditEventViewModel model)
    {
        if (ModelState.IsValid)
        {
            await _calendarService.UpdateEventAsync(model.CalendarId, model.Event);
            return RedirectToAction(nameof(AdminDashboard));
        }
        return View(model);
    }

    [Authorize(Roles = "Admin")]
    [HttpPost]
    public async Task<IActionResult> DeleteEvent(Guid eventId, Guid calendarId)
    {
        await _calendarService.DeleteEventAsync(calendarId, eventId);
        return RedirectToAction(nameof(AdminDashboard));
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
    public async Task<IActionResult> Create()
    {
        var countries = await _calendarService.GetDefaultCalendarCountriesAsync();
        var viewModel = new CreateCalendarViewModel
        {
            AvailableCountries = countries.Select(c => new SelectListItem
            {
                Value = c.CountryCode,
                Text = c.CountryName
            })
        };
        return View(viewModel);
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

            // Check if user is admin
            bool isAdmin = User.IsInRole("Admin");

            if (isAdmin && model.IsDefault)
            {
                // Admin creating a default calendar
                model.CreatedBy = long.Parse(userId);
                await _calendarService.CreateDefaultCalendarAsync(model);
            }
            else
            {
                // Regular user creating their calendar
                await _calendarService.CreateUserCalendarAsync(userId, model.Name);
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
                    HolidayCount = c.Holidays?.Count ?? 0,
                    CountryCode = c.Calendar.CountryCode // Add this line
                }).ToList(),
                IsAdmin = User.IsInRole("Admin") // Add this line
            };

            // If user is admin, add all default calendars
            if (viewModel.IsAdmin)
            {
                var defaultCalendars = await _calendarService.GetAllDefaultCalendarsAsync();
                viewModel.Calendars.AddRange(defaultCalendars.Select(c => new CalendarSummaryViewModel
                {
                    Id = c.Calendar.Id,
                    Name = c.Calendar.Name,
                    IsDefault = true,
                    ShareableLink = c.ShareableLink,
                    HolidayCount = c.Holidays?.Count ?? 0,
                    CountryCode = c.Calendar.CountryCode
                }));
            }

            return View(viewModel);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading dashboard for user {UserId}", User.FindFirstValue(ClaimTypes.NameIdentifier));
            TempData["Error"] = "An error occurred while loading your calendars.";
            return RedirectToAction("Error", "Home");
        }
    }

    [Authorize]
    [HttpPost]
    public async Task<IActionResult> CreateUserCalendar(CreateCalendarViewModel model)
    {
        if (ModelState.IsValid)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            await _calendarService.CreateUserCalendarAsync(userId, model.Name);
            return RedirectToAction(nameof(Dashboard));
        }
        return View(model);
    }

    [Authorize]
    [HttpPost]
    public async Task<IActionResult> EditUserCalendar(EditCalendarViewModel model)
    {
        if (ModelState.IsValid)
        {
            await _calendarService.UpdateCalendarAsync(model.Calendar);
            return RedirectToAction(nameof(Dashboard));
        }
        return View(model);
    }

    [Authorize]
    [HttpPost]
    public async Task<IActionResult> DeleteUserCalendar(Guid calendarId)
    {
        await _calendarService.DeleteCalendarAsync(calendarId);
        return RedirectToAction(nameof(Dashboard));
    }

    public async Task<IActionResult> View(Guid id, int? month = null, int? year = null)
    {
        try
        {
            var calendarDto = await _calendarService.GetCalendarByIdAsync(id);
            if (calendarDto == null)
                return NotFound();

            var currentDate = DateTime.Now;
            var viewModel = CalendarViewModel.FromDto(calendarDto,
                User.Identity.IsAuthenticated &&
                calendarDto.Calendar.CreatedBy.ToString() == User.FindFirstValue(ClaimTypes.NameIdentifier));

            viewModel.CurrentMonth = month ?? currentDate.Month;
            viewModel.CurrentYear = year ?? currentDate.Year;
            viewModel.UpdateNavigationDates();

            // Get events for the current month
            var events = await _calendarService.GetEventsByCalendarIdAsync(id);
            viewModel.Events = events.Where(e =>
                e.StartDate.Year == viewModel.CurrentYear &&
                e.StartDate.Month == viewModel.CurrentMonth).ToList();

            return View(viewModel);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error viewing calendar");
            return RedirectToAction("Error", "Home");
        }
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

    [HttpGet]
    public async Task<IActionResult> ExportToExcel(Guid calendarId, DateTime date)
    {
        try
        {
            var bytes = await _calendarService.ExportMonthToExcelAsync(calendarId, date);
            return File(
                bytes,
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                $"Calendar_Events_{date:MMMM_yyyy}.xlsx"
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error exporting to Excel for date {Date}", date);
            TempData["Error"] = "Failed to export to Excel. Please try again.";
            return RedirectToAction(nameof(Index), new { calendarId, date });
        }
    }

    [HttpGet]
    public async Task<IActionResult> ExportToPdf(Guid calendarId, DateTime date)
    {
        try
        {
            var bytes = await _calendarService.ExportMonthToPdfAsync(calendarId, date);
            return File(
                bytes,
                "application/pdf",
                $"Calendar_Events_{date:MMMM_yyyy}.pdf"
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error exporting to PDF for date {Date}", date);
            TempData["Error"] = "Failed to export to PDF. Please try again.";
            return RedirectToAction(nameof(Index), new { calendarId, date });
        }
    }

    [HttpGet]
    public async Task<IActionResult> ExportToCsv(Guid calendarId, DateTime date)
    {
        try
        {
            var bytes = await _calendarService.ExportMonthToCsvAsync(calendarId, date);
            return File(
                bytes,
                "application/vnd.ms-excel; charset=utf-8",
                $"Calendar_Events_{date:MMMM_yyyy}.csv"
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error exporting to CSV for date {Date}", date);
            TempData["Error"] = "Failed to export to CSV. Please try again.";
            return RedirectToAction(nameof(Index), new { calendarId, date });
        }
    }

    [HttpGet]
    public async Task<IActionResult> ExportToIcs(Guid calendarId, DateTime date)
    {
        try
        {
            var bytes = await _calendarService.ExportMonthToIcsAsync(calendarId, date);
            return File(
                bytes,
                "text/calendar",
                $"Calendar_Events_{date:MMMM_yyyy}.ics"
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error exporting to ICS for date {Date}", date);
            TempData["Error"] = "Failed to export to ICS. Please try again.";
            return RedirectToAction(nameof(Index), new { calendarId, date });
        }
    }

    [HttpGet]
    public async Task<IActionResult> ExportYearToExcel(Guid calendarId, int year)
    {
        try
        {
            var bytes = await _calendarService.ExportYearToExcelAsync(calendarId, year);
            return File(
                bytes,
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                $"Calendar_Events_{year}.xlsx"
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error exporting year to Excel {Year}", year);
            TempData["Error"] = "Failed to export to Excel. Please try again.";
            return RedirectToAction(nameof(Index), new { calendarId, year });
        }
    }

    [HttpGet]
    public async Task<IActionResult> ExportYearToPdf(Guid calendarId, int year)
    {
        try
        {
            var bytes = await _calendarService.ExportYearToPdfAsync(calendarId, year);
            return File(
                bytes,
                "application/pdf",
                $"Calendar_Events_{year}.pdf"
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error exporting year to PDF {Year}", year);
            TempData["Error"] = "Failed to export to PDF. Please try again.";
            return RedirectToAction(nameof(Index), new { calendarId, year });
        }
    }

    [HttpGet]
    public async Task<IActionResult> ExportYearToCsv(Guid calendarId, int year)
    {
        try
        {
            var bytes = await _calendarService.ExportYearToCsvAsync(calendarId, year);
            return File(
                bytes,
                "application/vnd.ms-excel; charset=utf-8",
                $"Calendar_Events_{year}.csv"
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error exporting year to CSV {Year}", year);
            TempData["Error"] = "Failed to export to CSV. Please try again.";
            return RedirectToAction(nameof(Index), new { calendarId, year });
        }
    }

    [HttpGet]
    public async Task<IActionResult> ExportYearToIcs(Guid calendarId, int year)
    {
        try
        {
            var bytes = await _calendarService.ExportYearToIcsAsync(calendarId, year);
            return File(
                bytes,
                "text/calendar",
                $"Calendar_Events_{year}.ics"
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error exporting year to ICS {Year}", year);
            TempData["Error"] = "Failed to export to ICS. Please try again.";
            return RedirectToAction(nameof(Index), new { calendarId, year });
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

    [Authorize]
    public async Task<IActionResult> GenerateShareableLink(Guid calendarId)
    {
        try
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var calendarDto = await _calendarService.GetCalendarByIdAsync(calendarId);

            if (calendarDto == null || (calendarDto.Calendar.CreatedBy.ToString() != userId && !User.IsInRole("Admin")))
                return NotFound();

            var shareableLink = await _calendarService.GenerateShareableLinkAsync(calendarId);

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

    [AllowAnonymous]
    public async Task<IActionResult> SharedCalendar(string shareableLink)
    {
        var calendarDto = await _calendarService.GetByShareableLinkAsync(shareableLink);
        if (calendarDto == null)
            return NotFound();

        var viewModel = new CalendarViewModel
        {
            Calendar = calendarDto.Calendar,
            Holidays = calendarDto.Holidays,
            Events = await _calendarService.GetEventsByCalendarIdAsync(calendarDto.Calendar.Id),
            IsEditable = false,
            ShareableLink = shareableLink
        };

        return View("Index", viewModel);
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
