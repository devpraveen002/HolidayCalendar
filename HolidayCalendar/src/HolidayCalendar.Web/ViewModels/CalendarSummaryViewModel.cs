namespace HolidayCalendar.src.HolidayCalendar.Web.ViewModels;

public class CalendarSummaryViewModel
{
    public Guid Id { get; set; }
    public string Name { get; set; }
    public bool IsDefault { get; set; }
    public string ShareableLink { get; set; }
    public int HolidayCount { get; set; }
    public string CountryCode { get; set; }
    public long CreatedBy { get; set; }
}
