using HolidayCalendar.src.HolidayCalendar.Core.Entities;
using HolidayCalendar.src.HolidayCalendar.Core.Services;
using HolidayCalendar.src.HolidayCalendar.Web.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
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
}
