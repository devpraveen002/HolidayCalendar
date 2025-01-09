using System.ComponentModel.DataAnnotations;

namespace HolidayCalendar.src.HolidayCalendar.Core.Entities;


public class Event : BaseEntity
{
    [Required]
    [StringLength(200)]
    public string Name { get; set; }
    [StringLength(500)]
    public string Description { get; set; }
    [Required]

    public DateTime StartDate { get; set; }
    [Required]
    public DateTime EndDate { get; set; }
    public Guid CalendarId { get; set; }
    public Calendar Calendar { get; set; }
    public Guid EventTypeId { get; set; }
    public EventType EventType { get; set; }
}
