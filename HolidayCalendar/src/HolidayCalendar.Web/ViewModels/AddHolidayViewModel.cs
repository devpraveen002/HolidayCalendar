using System.ComponentModel.DataAnnotations;

namespace HolidayCalendar.src.HolidayCalendar.Web.ViewModels;

public class AddHolidayViewModel
{
    [Required]
    public string Name { get; set; }

    [Required]
    public DateTime Date { get; set; }

    public bool IsFixedHoliday { get; set; }
    public bool IsWeekendAdjustable { get; set; }
    public int CalendarId { get; set; }
}
