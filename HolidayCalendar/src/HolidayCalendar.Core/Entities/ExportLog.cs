namespace HolidayCalendar.src.HolidayCalendar.Core.Entities;

public class ExportLog : BaseEntity
{
    public Guid Id { get; set; }
    public Guid CalendarId { get; set; }
    public string ExportType { get; set; }
    public DateTime ExportDate { get; set; }
    public string ExportedBy { get; set; }
    public Calendar Calendar { get; set; }
    public string ExportFormat { get; set; }
}
