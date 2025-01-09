namespace HolidayCalendar.src.HolidayCalendar.Core.Entities;

public class Holiday : BaseEntity
{
    public string Name { get; set; }
    public DateTime Date { get; set; }
    public bool IsFixedHoliday { get; set; }
    public bool IsWeekendAdjustable { get; set; }
    public string? Description { get; set; }
    public bool IsDefault { get; set; }
}
