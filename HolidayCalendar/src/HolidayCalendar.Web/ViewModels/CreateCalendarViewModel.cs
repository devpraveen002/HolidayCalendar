using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;

namespace HolidayCalendar.src.HolidayCalendar.Web.ViewModels;

public class CreateCalendarViewModel
{
    [Required]
    [Display(Name = "Calendar Name")]
    [StringLength(100, ErrorMessage = "Calendar name cannot be longer than 100 characters.")]
    public string Name { get; set; }
    public string CountryCode { get; set; }
    public bool IsDefault { get; set; }
    public long CreatedBy { get; set; }

    public IEnumerable<SelectListItem> AvailableCountries { get; set; }

    public CreateCalendarViewModel()
    {
        AvailableCountries = new List<SelectListItem>();
    }
}
