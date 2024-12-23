using HolidayCalendar.src.HolidayCalendar.Core.Entities;

namespace HolidayCalendar.src.HolidayCalendar.Web.ViewModels;

public class CalendarViewModel
{
    public Calendar Calendar { get; set; }
    public bool IsEditable { get; set; }
    public string ShareableLink { get; set; }
    public string Description { get; set; }
    public int CurrentMonth { get; set; }
    public int CurrentYear { get; set; }
    public int PreviousMonth { get; set; }
    public int PreviousYear { get; set; }
    public int NextMonth { get; set; }
    public int NextYear { get; set; }
}
