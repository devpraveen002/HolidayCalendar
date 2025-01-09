namespace HolidayCalendar.src.HolidayCalendar.Core.Entities;

public class Country : BaseEntity
{
    public string CountryCode { get; set; }
    public string CountryName { get; set; }
    public bool IsActive { get; set; }
    public string? Description { get; set; }
    public int DisplayOrder { get; set; }
}
