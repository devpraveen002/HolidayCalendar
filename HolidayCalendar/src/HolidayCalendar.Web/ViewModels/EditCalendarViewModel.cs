using HolidayCalendar.src.HolidayCalendar.Core.Entities;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;

namespace HolidayCalendar.src.HolidayCalendar.Web.ViewModels;

public class EditCalendarViewModel
{
     public Guid CalendarId { get; set; }

    [Required]
    [StringLength(100, ErrorMessage = "Name must be less than 100 characters.")]
    public string Name { get; set; }

    [Required(ErrorMessage = "Country is required.")]
    public string SelectedCountry { get; set; }

    public List<SelectListItem> AvailableCountries { get; set; } = new List<SelectListItem>();
}
