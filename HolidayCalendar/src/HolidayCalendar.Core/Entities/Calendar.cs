namespace HolidayCalendar.src.HolidayCalendar.Core.Entities;

public class Calendar
{
    public int Id { get; set; }
    public string Name { get; set; }
    public string ShareableLink { get; set; }
    public bool IsDefault { get; set; }
    public string? UserId { get; set; }  
    public User? User { get; set; }   
    public ICollection<Holiday> Holidays { get; set; } = new List<Holiday>();
}
