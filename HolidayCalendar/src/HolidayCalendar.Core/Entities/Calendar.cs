namespace HolidayCalendar.src.HolidayCalendar.Core.Entities;

public class Calendar : BaseEntity
{
    public string Name { get; set; }
    public string ShareableLink { get; set; }
    public bool IsDefault { get; set; }
    //public int Id { get; set; }
    //public string Name { get; set; }
    //public string ShareableLink { get; set; }
    //public bool IsDefault { get; set; }
    //public ICollection<Holiday> Holidays { get; set; } = new List<Holiday>();
}
