using System.ComponentModel.DataAnnotations;

namespace HolidayCalendar.src.HolidayCalendar.Web.ViewModels;

public class CreateCalendarViewModel
{
    [Required]
    [Display(Name = "Calendar Name")]
    [StringLength(100, ErrorMessage = "Calendar name cannot be longer than 100 characters.")]
    public string Name { get; set; }
}
