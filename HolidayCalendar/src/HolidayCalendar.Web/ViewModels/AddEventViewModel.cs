using System.ComponentModel.DataAnnotations;

namespace HolidayCalendar.src.HolidayCalendar.Web.ViewModels;

public class AddEventViewModel
{
    [Required]
    public Guid CalendarId { get; set; }

    [Required]
    [StringLength(100, ErrorMessage = "Event name must be under 100 characters.")]
    public string Name { get; set; }

    [StringLength(500, ErrorMessage = "Event description must be under 500 characters.")]
    public string Description { get; set; }

    [Required]
    public DateTime StartDate { get; set; }

    [Required]
    public DateTime EndDate { get; set; }
}
