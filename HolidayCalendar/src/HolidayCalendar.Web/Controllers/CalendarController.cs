using HolidayCalendar.src.HolidayCalendar.Core.Entities;
using HolidayCalendar.src.HolidayCalendar.Core.Services;
using HolidayCalendar.src.HolidayCalendar.Web.ViewModels;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using System.Security.Claims;
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

    [AllowAnonymous]
    public async Task<IActionResult> Index(Guid? id = null, string SelectedCountry = "US", int? month = null, int? year = null)
    {
        try
        {
            var currentDate = DateTime.Now;
            var currentMonth = month ?? currentDate.Month;
            var currentYear = year ?? currentDate.Year;

            CalendarDto calendarDto;

            // If id is provided, fetch the calendar by ID; otherwise, fetch by country
            if (id.HasValue)
            {
                calendarDto = await _calendarService.GetCalendarByIdAsync(id.Value);
                if (calendarDto == null)
                {
                    _logger.LogWarning("No calendar found for id: {Id}", id.Value);
                    TempData["Error"] = "Calendar not found.";
                    return RedirectToAction("Error", "Home");
                }
            }
            else
            {
                calendarDto = await _calendarService.GetDefaultCalendarByCountryAsync(SelectedCountry);
                if (calendarDto == null)
                {
                    _logger.LogWarning("No default calendar found for country code: {CountryCode}", SelectedCountry);
                    TempData["Error"] = "Calendar not found for the selected country.";
                    return RedirectToAction("Error", "Home");
                }
            }

            var countries = await _calendarService.GetDefaultCalendarCountriesAsync();

            var events = await _calendarService.GetEventsByCalendarIdAsync(calendarDto.Calendar.Id);

            var viewModel = new CalendarViewModel
            {
                Calendar = calendarDto.Calendar,
                Holidays = calendarDto.Holidays ?? new List<Holiday>(),
                Events = events ?? new List<Event>(),
                CurrentMonth = currentMonth,
                CurrentYear = currentYear,
                SelectedCountry = calendarDto.Calendar.CountryCode,
                IsEditable = User.Identity.IsAuthenticated && User.IsInRole("Admin"),
                ShareableLink = calendarDto.ShareableLink,
                AvailableCountries = countries.Select(c => new SelectListItem
                {
                    Value = c.CountryCode,
                    Text = c.CountryName,
                    Selected = c.CountryCode == calendarDto.Calendar.CountryCode
                }).ToList()
            };

            viewModel.UpdateNavigationDates();

            return View(viewModel);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading calendar for country code: {SelectedCountry} or id: {Id}", SelectedCountry, id);
            TempData["Error"] = "An error occurred while loading the calendar.";
            return RedirectToAction("Error", "Home");
        }
    }

    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> AdminDashboard()
    {
        try
        {
            var calendars = await _calendarService.GetAllDefaultCalendarsAsync();

            var viewModel = calendars.Select(c => new CalendarSummaryViewModel
            {
                Id = c.Calendar.Id,
                Name = c.Calendar.Name,
                ShareableLink = c.ShareableLink,
                HolidayCount = c.Holidays?.Count ?? 0,
                CountryCode = c.Calendar.CountryCode,
                IsDefault = c.Calendar.IsDefault,
                CreatedBy = c.Calendar.CreatedBy
            });

            return View(viewModel);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading admin dashboard");
            TempData["Error"] = "An error occurred while loading the dashboard.";
            return RedirectToAction("Error", "Home");
        }
    }

    [Authorize(Roles = "Admin")]
    [HttpGet]
    public IActionResult CreateCountry()
    {
        var countries = new List<SelectListItem>
    {
        new SelectListItem { Value = "US", Text = "United States" },
        new SelectListItem { Value = "CA", Text = "Canada" },
        new SelectListItem { Value = "UK", Text = "United Kingdom" },
        new SelectListItem { Value = "AU", Text = "Australia" },
        // Add more countries if needed
    };

        var viewModel = new CreateCountryViewModel
        {
            AvailableCountries = countries
        };

        return View(viewModel);
    }


    [Authorize(Roles = "Admin")]
    [HttpPost]
    public async Task<IActionResult> CreateCountry(CreateCountryViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        try
        {
            var country = new Country
            {
                CountryCode = model.SelectedCountryCode,
                CountryName = model.SelectedCountryName,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = long.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier))
            };

            await _calendarService.AddCountryAsync(country);

            TempData["Success"] = "Country added successfully!";
            return RedirectToAction(nameof(AdminDashboard));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding country");
            TempData["Error"] = "An error occurred while adding the country.";
            return View(model);
        }
    }

    //[Authorize(Roles = "Admin")]
    //[HttpPost]
    //public async Task<IActionResult> EditEvent(EditEventViewModel model)
    //{
    //    if (ModelState.IsValid)
    //    {
    //        await _calendarService.UpdateEventAsync(model.CalendarId, model.Event);
    //        return RedirectToAction(nameof(AdminDashboard));
    //    }
    //    return View(model);
    //}
    [Authorize]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditEvent(EditEventViewModel model)
    {
        if (!ModelState.IsValid)
        {
            TempData["Error"] = "Invalid event data.";
            return RedirectToAction("View", new { id = model.CalendarId });
        }

        try
        {
            var userId = long.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
            var isAdmin = User.IsInRole("Admin");

            var eventEntity = new Event
            {
                Id = model.EventId,
                CalendarId = model.CalendarId,
                Name = model.Name,
                Description = model.Description,
                StartDate = model.StartDate,
                EndDate = model.EndDate,
                ModifiedAt = DateTime.UtcNow,
                ModifiedBy = userId
            };

            await _calendarService.UpdateEventAsync(model.CalendarId, eventEntity, userId, isAdmin);

            TempData["Success"] = "Event updated successfully!";
            return RedirectToAction("View", new { id = model.CalendarId });
        }
        catch (UnauthorizedAccessException)
        {
            TempData["Error"] = "You are not authorized to edit this event.";
            return RedirectToAction("View", new { id = model.CalendarId });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error editing event");
            TempData["Error"] = "An error occurred while editing the event.";
            return RedirectToAction("View", new { id = model.CalendarId });
        }
    }

    //[Authorize(Roles = "Admin")]
    //[HttpPost]
    //public async Task<IActionResult> DeleteEvent(Guid eventId, Guid calendarId)
    //{
    //    await _calendarService.DeleteEventAsync(calendarId, eventId);
    //    return RedirectToAction(nameof(AdminDashboard));
    //}

    [Authorize]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteEvent(DeleteEventViewModel model)
    {
        try
        {
            var userId = long.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
            var isAdmin = User.IsInRole("Admin");

            await _calendarService.DeleteEventAsync(model.CalendarId, model.EventId, userId, isAdmin);

            TempData["Success"] = "Event deleted successfully!";
            return RedirectToAction("View", new { id = model.CalendarId });
        }
        catch (UnauthorizedAccessException)
        {
            TempData["Error"] = "You are not authorized to delete this event.";
            return RedirectToAction("View", new { id = model.CalendarId });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting event");
            TempData["Error"] = "An error occurred while deleting the event.";
            return RedirectToAction("View", new { id = model.CalendarId });
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

            var isAdmin = User.IsInRole("Admin");

            if (isAdmin && model.IsDefault)
            {
                // Admin creating a default calendar
                model.CreatedBy = long.Parse(userId);
                await _calendarService.CreateDefaultCalendarAsync(model);
            }
            else
            {
                // Regular user creating their calendar
                await _calendarService.CreateUserCalendarAsync(userId, model.Name, model.CountryCode);
            }

            TempData["Success"] = "Calendar created successfully!";
            return RedirectBasedOnRole();
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
            var calendarDto = await _calendarService.GetCalendarByIdAsync(id);

            if (calendarDto == null)
            {
                _logger.LogWarning("Calendar not found for id: {Id}", id);
                TempData["Error"] = "Calendar not found.";
                return RedirectBasedOnRole();
            }

            var countries = await _calendarService.GetDefaultCalendarCountriesAsync();

            var viewModel = new EditCalendarViewModel
            {
                CalendarId = calendarDto.Calendar.Id,
                Name = calendarDto.Calendar.Name,
                SelectedCountry = calendarDto.Calendar.CountryCode,
                AvailableCountries = countries.Select(c => new SelectListItem
                {
                    Value = c.CountryCode,
                    Text = c.CountryName,
                    Selected = c.CountryCode == calendarDto.Calendar.CountryCode
                }).ToList()
            };

            return View(viewModel);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading calendar for edit with id: {Id}", id);
            TempData["Error"] = "An error occurred while loading the calendar.";
            return RedirectBasedOnRole();
        }
    }



    [Authorize]
    [HttpPost]
    public async Task<IActionResult> Edit(EditCalendarViewModel model)
    {
        if (!ModelState.IsValid)
        {
            _logger.LogWarning("Model state invalid while editing calendar with ID: {CalendarId}", model.CalendarId);
            TempData["Error"] = "Invalid data provided.";
            return View(model);
        }

        try
        {
            var userId = long.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
            var isAdmin = User.IsInRole("Admin");

            // Update the calendar
            await _calendarService.UpdateCalendarAsync(model.CalendarId, model.Name, model.SelectedCountry, userId, isAdmin);

            TempData["Success"] = "Calendar updated successfully!";
            return RedirectBasedOnRole();
        }
        catch (UnauthorizedAccessException)
        {
            TempData["Error"] = "You are not authorized to edit this calendar.";
            return RedirectBasedOnRole();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating calendar with ID: {CalendarId}", model.CalendarId);
            TempData["Error"] = "An error occurred while updating the calendar.";
            return RedirectBasedOnRole();
        }
    }


    [Authorize]
    [HttpGet]
    public async Task<IActionResult> Delete(Guid id)
    {
        var calendarDto = await _calendarService.GetCalendarByIdAsync(id);

        if (calendarDto == null)
        {
            TempData["Error"] = "Calendar not found.";
            return RedirectToAction(nameof(Dashboard));
        }

        return View(calendarDto); // Redirects to the Delete confirmation view
    }


    [Authorize]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(Guid id)
    {
        try
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var isAdmin = User.IsInRole("Admin");

            var calendarDto = await _calendarService.GetCalendarByIdAsync(id);

            if (calendarDto == null || (!isAdmin && calendarDto.Calendar.CreatedBy.ToString() != userId))
            {
                TempData["Error"] = "You are not authorized to delete this calendar.";
                return RedirectBasedOnRole();
            }

            if (calendarDto.Calendar.IsDefault)
            {
                TempData["Error"] = "Cannot delete the default calendar.";
                return RedirectBasedOnRole();
            }

            await _calendarService.DeleteCalendarAsync(id);
            TempData["Success"] = "Calendar deleted successfully!";
            return RedirectBasedOnRole();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting calendar");
            TempData["Error"] = "An error occurred while deleting the calendar.";
            return RedirectBasedOnRole();
        }
    }



    [Authorize]
    public async Task<IActionResult> Dashboard()
    {
        try
        {
            var userId = long.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));

            var calendars = await _calendarService.GetUserCalendarsAsync(userId.ToString());

            var viewModel = calendars.Select(c => new CalendarSummaryViewModel
            {
                Id = c.Calendar.Id,
                Name = c.Calendar.Name,
                ShareableLink = c.ShareableLink,
                HolidayCount = c.Holidays?.Count ?? 0,
                CountryCode = c.Calendar.CountryCode,
                IsDefault = c.Calendar.IsDefault,
                CreatedBy = c.Calendar.CreatedBy
            });

            return View(viewModel);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading user dashboard");
            TempData["Error"] = "An error occurred while loading the dashboard.";
            return RedirectToAction("Error", "Home");
        }
    }



    private IActionResult RedirectBasedOnRole()
    {
        return User.IsInRole("Admin") ? RedirectToAction(nameof(AdminDashboard)) : RedirectToAction(nameof(Dashboard));
    }


    [Authorize]
    [HttpGet]
    public async Task<IActionResult> CreateUserCalendar()
    {
        var countries = await _calendarService.GetDefaultCalendarCountriesAsync();
        var viewModel = new CreateCalendarViewModel
        {
            AvailableCountries = countries.Select(c => new SelectListItem
            {
                Value = c.CountryCode,
                Text = c.CountryName
            }).ToList()
        };

        return View(viewModel);
    }

    [Authorize]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateUserCalendar(CreateCalendarViewModel model)
    {
        if (!ModelState.IsValid)
        {
            var countries = await _calendarService.GetDefaultCalendarCountriesAsync();
            model.AvailableCountries = countries.Select(c => new SelectListItem
            {
                Value = c.CountryCode,
                Text = c.CountryName
            }).ToList();
            return View(model);
        }

        try
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                return RedirectToAction("Login", "Account");
            }

            // Pass CountryCode to the service
            await _calendarService.CreateUserCalendarAsync(userId, model.Name, model.CountryCode);

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
    public async Task<IActionResult> EditUserCalendar(Guid id)
    {
        try
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var calendarDto = await _calendarService.GetCalendarByIdAsync(id);

            if (calendarDto == null || calendarDto.Calendar.CreatedBy.ToString() != userId)
            {
                TempData["Error"] = "You are not authorized to edit this calendar.";
                return RedirectToAction(nameof(Dashboard));
            }

            var countries = await _calendarService.GetDefaultCalendarCountriesAsync();

            var viewModel = new EditCalendarViewModel
            {
                CalendarId = calendarDto.Calendar.Id,
                Name = calendarDto.Calendar.Name,
                SelectedCountry = calendarDto.Calendar.CountryCode,
                AvailableCountries = countries.Select(c => new SelectListItem
                {
                    Value = c.CountryCode,
                    Text = c.CountryName,
                    Selected = c.CountryCode == calendarDto.Calendar.CountryCode
                }).ToList()
            };

            return View("Edit", viewModel);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading calendar for edit with id: {Id}", id);
            TempData["Error"] = "An error occurred while loading the calendar.";
            return RedirectToAction(nameof(Dashboard));
        }
    }


    [Authorize]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditUserCalendar(EditCalendarViewModel model)
    {
        if (!ModelState.IsValid)
        {
            TempData["Error"] = "Invalid data provided.";
            return View("Edit", model);
        }

        try
        {
            var userId = long.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));

            var currentCalendar = await _calendarService.GetCalendarByIdAsync(model.CalendarId);

            if (currentCalendar.Calendar.CreatedBy != userId)
            {
                TempData["Error"] = "You are not authorized to edit this calendar.";
                return RedirectToAction(nameof(Dashboard));
            }

            // Use the existing country code from the database
            var selectedCountry = currentCalendar.Calendar.CountryCode;

            var updatedCalendar = await _calendarService.UpdateCalendarAsync(
                model.CalendarId,
                model.Name,
                selectedCountry, // Use the country code from the database
                userId,
                isAdmin: false
            );

            TempData["Success"] = "Calendar updated successfully!";
            return RedirectToAction(nameof(Dashboard));
        }
        catch (UnauthorizedAccessException)
        {
            TempData["Error"] = "You are not authorized to edit this calendar.";
            return RedirectToAction(nameof(Dashboard));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating calendar with ID: {CalendarId}", model.CalendarId);
            TempData["Error"] = "An error occurred while updating the calendar.";
            return RedirectToAction(nameof(Dashboard));
        }
    }



    [Authorize]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteUserCalendar(Guid calendarId)
    {
        try
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var calendarDto = await _calendarService.GetCalendarByIdAsync(calendarId);

            if (calendarDto == null || calendarDto.Calendar.CreatedBy.ToString() != userId)
            {
                TempData["Error"] = "You are not authorized to delete this calendar.";
                return RedirectToAction(nameof(Dashboard));
            }

            await _calendarService.DeleteCalendarAsync(calendarId);

            TempData["Success"] = "Calendar deleted successfully!";
            return RedirectToAction(nameof(Dashboard));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting calendar with ID: {CalendarId}", calendarId);
            TempData["Error"] = "An error occurred while deleting the calendar.";
            return RedirectToAction(nameof(Dashboard));
        }
    }

    [Authorize]
    public async Task<IActionResult> View(Guid id, int? month = null, int? year = null)
    {
        try
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var calendarDto = await _calendarService.GetCalendarByIdAsync(id);

            if (calendarDto == null || calendarDto.Calendar.CreatedBy.ToString() != userId)
            {
                return Unauthorized();
            }

            var currentDate = DateTime.Now;
            var viewModel = CalendarViewModel.FromDto(calendarDto, true);

            viewModel.CurrentMonth = month ?? currentDate.Month;
            viewModel.CurrentYear = year ?? currentDate.Year;

            // Filter dropdown for user's calendars
            var userCalendars = await _calendarService.GetUserCalendarsAsync(userId);
            viewModel.AvailableCountries = userCalendars.Select(c => new SelectListItem
            {
                Value = c.Calendar.CountryCode,
                Text = c.Calendar.Name,
                Selected = c.Calendar.Id == id
            }).ToList();

            // Fetch holidays specific to the calendar
            viewModel.Holidays = await _calendarService.GetHolidaysByCalendarIdAsync(id);
            viewModel.Events = await _calendarService.GetEventsByCalendarIdAsync(id);

            return View(viewModel);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading calendar view for id {Id}", id);
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

    [Authorize]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddEvent(AddEventViewModel model)
    {
        if (!ModelState.IsValid)
        {
            TempData["Error"] = "Invalid event data.";
            return RedirectToAction("View", new { id = model.CalendarId });
        }

        try
        {
            var eventEntity = new Event
            {
                Id = Guid.NewGuid(),
                CalendarId = model.CalendarId,
                Name = model.Name,
                Description = model.Description,
                StartDate = model.StartDate,
                EndDate = model.EndDate,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = long.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier))
            };

            await _calendarService.AddEventAsync(model.CalendarId, eventEntity);

            TempData["Success"] = "Event added successfully!";
            return RedirectToAction("View", new { id = model.CalendarId });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding event");
            TempData["Error"] = "An error occurred while adding the event.";
            return RedirectToAction("View", new { id = model.CalendarId });
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
