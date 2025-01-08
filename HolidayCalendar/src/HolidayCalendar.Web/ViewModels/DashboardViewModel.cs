using HolidayCalendar.src.HolidayCalendar.Core.DTOs;

namespace HolidayCalendar.src.HolidayCalendar.Web.ViewModels;

public class DashboardViewModel
{
    public ICollection<CalendarSummaryViewModel> Calendars { get; set; } = new List<CalendarSummaryViewModel>();

    public static DashboardViewModel FromDto(IEnumerable<CalendarDto> calendars)
    {
        return new DashboardViewModel
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
    }
}
