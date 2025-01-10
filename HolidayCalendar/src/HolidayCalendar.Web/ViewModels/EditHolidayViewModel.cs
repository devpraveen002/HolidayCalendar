using System.ComponentModel.DataAnnotations;

namespace HolidayCalendar.src.HolidayCalendar.Web.ViewModels;

public class EditHolidayViewModel
{
    [Required]
    public Guid HolidayId { get; set; }

    [Required]
    public string Name { get; set; }

    [Required]
    public DateTime Date { get; set; }

    public bool IsFixedHoliday { get; set; }
    public bool IsWeekendAdjustable { get; set; }

    [Required]
    public Guid CalendarId { get; set; }
}
