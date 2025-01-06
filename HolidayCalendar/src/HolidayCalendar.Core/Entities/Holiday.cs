namespace HolidayCalendar.src.HolidayCalendar.Core.Entities;

public class Holiday
{
    public int Id { get; set; }
    public string Name { get; set; }
    public DateTime Date { get; set; }
    public bool IsFixedHoliday { get; set; }
    public bool IsWeekendAdjustable { get; set; }
    public int CalendarId { get; set; }
    public Calendar Calendar { get; set; }
    public string? Description { get; set; }
}
