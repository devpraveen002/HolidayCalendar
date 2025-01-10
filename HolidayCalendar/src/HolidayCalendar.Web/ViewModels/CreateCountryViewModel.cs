using Microsoft.AspNetCore.Mvc.Rendering;

namespace HolidayCalendar.src.HolidayCalendar.Web.ViewModels;

public class CreateCountryViewModel
{
    public string SelectedCountryCode { get; set; }
    public string SelectedCountryName { get; set; }
    public IEnumerable<SelectListItem> AvailableCountries { get; set; }

    public CreateCountryViewModel()
    {
        AvailableCountries = new List<SelectListItem>();
    }
}
