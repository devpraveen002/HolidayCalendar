using System.ComponentModel.DataAnnotations;

namespace HolidayCalendar.src.HolidayCalendar.Web.ViewModels;

public class DeleteEventViewModel
{
    [Required]
    public Guid EventId { get; set; }

    [Required]
    public Guid CalendarId { get; set; }
}
