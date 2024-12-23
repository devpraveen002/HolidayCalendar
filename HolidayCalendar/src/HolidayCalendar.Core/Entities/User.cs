using Microsoft.AspNetCore.Identity;

namespace HolidayCalendar.src.HolidayCalendar.Core.Entities;

public class User : IdentityUser
{
    public ICollection<Calendar> Calendars { get; set; } = new List<Calendar>();
}
