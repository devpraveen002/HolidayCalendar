using HolidayCalendar.src.HolidayCalendar.Core.DTOs;
using HolidayCalendar.src.HolidayCalendar.Core.Entities;

namespace HolidayCalendar.src.HolidayCalendar.Web.ViewModels;

public class CalendarViewModel
{
    public Calendar Calendar { get; set; }
    public IEnumerable<Holiday> Holidays { get; set; }
    public bool IsEditable { get; set; }
    public string ShareableLink { get; set; }
    public string Description { get; set; }
    public int CurrentMonth { get; set; }
    public int CurrentYear { get; set; }
    public int PreviousMonth { get; set; }
    public int PreviousYear { get; set; }
    public int NextMonth { get; set; }
    public int NextYear { get; set; }
    public int SelectedYear { get; set; }

    public static CalendarViewModel FromDto(CalendarDto dto, bool isEditable)
    {
        var currentDate = DateTime.Now;
        var model = new CalendarViewModel
        {
            Calendar = dto.Calendar,
            Holidays = dto.Holidays?.ToList() ?? new List<Holiday>(),
            IsEditable = isEditable,
            ShareableLink = dto.ShareableLink,
            CurrentMonth = currentDate.Month,
            CurrentYear = currentDate.Year,
            SelectedYear = currentDate.Year
        };

        var currentMonth = new DateTime(model.CurrentYear, model.CurrentMonth, 1);
        model.PreviousMonth = currentMonth.AddMonths(-1).Month;
        model.PreviousYear = currentMonth.AddMonths(-1).Year;
        model.NextMonth = currentMonth.AddMonths(1).Month;
        model.NextYear = currentMonth.AddMonths(1).Year;

        return model;
    }
}
//public class CalendarViewModel
//{
//    public Calendar Calendar { get; set; }
//    public bool IsEditable { get; set; }
//    public string ShareableLink { get; set; }
//    public string Description { get; set; }
//    public int CurrentMonth { get; set; }
//    public int CurrentYear { get; set; }
//    public int PreviousMonth { get; set; }
//    public int PreviousYear { get; set; }
//    public int NextMonth { get; set; }
//    public int NextYear { get; set; }
//    public int SelectedYear { get; set; }
//}
