namespace HolidayCalendar.src.HolidayCalendar.Core.Entities;

public class CalendarShare : BaseEntity
{
    public Guid Id { get; set; }
    public Guid CalendarId { get; set; }
    public string ShareableLink { get; set; }
    public DateTime ExpiryDate { get; set; }
    public bool IsActive { get; set; }
    public Calendar Calendar { get; set; }
}
