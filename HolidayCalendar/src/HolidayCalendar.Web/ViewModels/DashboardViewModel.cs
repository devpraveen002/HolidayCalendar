using HolidayCalendar.src.HolidayCalendar.Core.Entities;

namespace HolidayCalendar.src.HolidayCalendar.Web.ViewModels;

public class DashboardViewModel
{
    public IEnumerable<Calendar> Calendars { get; set; } = new List<Calendar>();
}
