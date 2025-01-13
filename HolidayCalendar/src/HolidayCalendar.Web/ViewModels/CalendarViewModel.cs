using HolidayCalendar.src.HolidayCalendar.Core.DTOs;
using HolidayCalendar.src.HolidayCalendar.Core.Entities;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace HolidayCalendar.src.HolidayCalendar.Web.ViewModels;

public class CalendarViewModel
{
    public Calendar Calendar { get; set; }
    public IEnumerable<Event> Events { get; set; } = new List<Event>();
    public IEnumerable<Holiday> Holidays { get; set; }
    public bool IsEditable { get; set; }
    public string ShareableLink { get; set; }
    public string Description { get; set; }
    public int CurrentMonth { get; set; }
    public int CurrentYear { get; set; }
    public int PreviousMonth { get; set; }
    public int PreviousYear { get; set; }
    public int NextMonth { get; set; }
    public int NextYear { get; set; }
    public int SelectedYear { get; set; }
    public string SelectedCountry { get; set; }
    public IEnumerable<SelectListItem> AvailableCountries { get; set; }
    public IEnumerable<SelectListItem> AvailableMonths { get; set; }
    public IEnumerable<SelectListItem> AvailableYears { get; set; }
    public IEnumerable<SelectListItem> AvailableCalendars { get; set; }

    public CalendarViewModel()
    {
        var currentDate = DateTime.Now;
        CurrentMonth = currentDate.Month;
        CurrentYear = currentDate.Year;
        SelectedYear = currentDate.Year;

        // Initialize the collections
        AvailableMonths = Enumerable.Range(1, 12).Select(m => new SelectListItem
        {
            Value = m.ToString(),
            Text = new DateTime(2000, m, 1).ToString("MMMM")
        });

        AvailableYears = Enumerable.Range(currentDate.Year - 5, 11).Select(y => new SelectListItem
        {
            Value = y.ToString(),
            Text = y.ToString()
        });

        // Initialize AvailableCountries as empty SelectListItem collection
        AvailableCountries = new List<SelectListItem>();
    }
    public static CalendarViewModel FromDto(CalendarDto dto, bool isEditable, IEnumerable<SelectListItem> availableCalendars)
    {
        var currentDate = DateTime.Now;
        var model = new CalendarViewModel
        {
            Calendar = dto.Calendar,
            Holidays = dto.Holidays?.ToList() ?? new List<Holiday>(),
            IsEditable = isEditable,
            ShareableLink = dto.ShareableLink,
            CurrentMonth = currentDate.Month,
            CurrentYear = currentDate.Year,
            SelectedYear = currentDate.Year,
            AvailableCalendars = availableCalendars // Assign the calendars
        };

        var currentMonth = new DateTime(model.CurrentYear, model.CurrentMonth, 1);
        model.PreviousMonth = currentMonth.AddMonths(-1).Month;
        model.PreviousYear = currentMonth.AddMonths(-1).Year;
        model.NextMonth = currentMonth.AddMonths(1).Month;
        model.NextYear = currentMonth.AddMonths(1).Year;

        return model;
    }

    public void UpdateNavigationDates()
    {
        var currentDate = new DateTime(CurrentYear, CurrentMonth, 1);

        var previousMonth = currentDate.AddMonths(-1);
        PreviousMonth = previousMonth.Month;
        PreviousYear = previousMonth.Year;

        var nextMonth = currentDate.AddMonths(1);
        NextMonth = nextMonth.Month;
        NextYear = nextMonth.Year;
    }

    public List<CalendarDayViewModel> GetDays()
    {
        var days = new List<CalendarDayViewModel>();
        var firstDayOfMonth = new DateTime(CurrentYear, CurrentMonth, 1);
        var daysInMonth = DateTime.DaysInMonth(CurrentYear, CurrentMonth);

        for (int i = 0; i < daysInMonth; i++)
        {
            var date = firstDayOfMonth.AddDays(i);
            var holidays = Holidays?.Where(h => h.Date.Date == date.Date).ToList() ?? new List<Holiday>();
            var events = Events?.Where(e => e.StartDate.Date == date.Date).ToList() ?? new List<Event>();

            days.Add(new CalendarDayViewModel
            {
                Date = date,
                Holidays = holidays,
                Events = events
            });
        }

        return days;
    }
}