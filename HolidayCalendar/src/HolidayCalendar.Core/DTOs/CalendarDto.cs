using HolidayCalendar.src.HolidayCalendar.Core.Entities;

namespace HolidayCalendar.src.HolidayCalendar.Core.DTOs;


public class CalendarDto
{
    public Calendar Calendar { get; set; }
    public List<Holiday> Holidays { get; set; }
    public string ShareableLink { get; set; }
}
