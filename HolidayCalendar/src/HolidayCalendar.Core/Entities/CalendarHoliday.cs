namespace HolidayCalendar.src.HolidayCalendar.Core.Entities;

public class CalendarHoliday : BaseEntity
{
    public Guid CalendarId { get; set; }
    public Guid HolidayId { get; set; }
    public Calendar Calendar { get; set; }
    public Holiday Holiday { get; set; }
}
