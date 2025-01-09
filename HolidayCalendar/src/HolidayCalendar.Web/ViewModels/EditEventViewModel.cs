using HolidayCalendar.src.HolidayCalendar.Core.Entities;

namespace HolidayCalendar.src.HolidayCalendar.Web.ViewModels;

public class EditEventViewModel
{
    public Guid CalendarId { get; set; }
    public Event Event { get; set; }
}
