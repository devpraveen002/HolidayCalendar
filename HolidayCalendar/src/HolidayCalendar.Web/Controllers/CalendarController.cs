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
                calendar = await _calendarService.GetCalendarByShareableLinkAsync(shareableLink);
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
    public async Task<IActionResult> Create()
    {
        try
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            // Use CreateUserCalendarAsync instead of CreateCalendarAsync
            var newCalendar = await _calendarService.CreateUserCalendarAsync(userId);
            return RedirectToAction(nameof(Dashboard));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating calendar");
            return RedirectToAction("Error", "Home");
        }
    }

    [Authorize]
    public async Task<IActionResult> Dashboard()
    {
        try
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var calendars = await _calendarService.GetUserCalendarsAsync(userId);

            var viewModel = new DashboardViewModel
            {
                Calendars = calendars
            };

            return View(viewModel);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading dashboard");
            return RedirectToAction("Error", "Home");
        }
    }
}
