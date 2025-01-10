using HolidayCalendar.src.HolidayCalendar.Core.Entities;

namespace HolidayCalendar.src.HolidayCalendar.Web.ViewModels;

public class CalendarDayViewModel
{
    public DateTime Date { get; set; }
    public List<Holiday> Holidays { get; set; }
    public List<Event> Events { get; set; }

    public CalendarDayViewModel()
    {
        Holidays = new List<Holiday>();
        Events = new List<Event>();
    }
}
