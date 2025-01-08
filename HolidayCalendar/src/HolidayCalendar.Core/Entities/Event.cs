namespace HolidayCalendar.src.HolidayCalendar.Core.Entities;


public class Event : BaseEntity
{
    public string Name { get; set; }
    public string Description { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public Guid CalendarId { get; set; }
    public Calendar Calendar { get; set; }
}
