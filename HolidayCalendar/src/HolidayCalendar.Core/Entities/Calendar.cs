using System.ComponentModel.DataAnnotations;

namespace HolidayCalendar.src.HolidayCalendar.Core.Entities;

public class Calendar : BaseEntity
{
    [Required]
    [StringLength(100)]
    public string Name { get; set; }
    [StringLength(100)]
    public string ShareableLink { get; set; }
    public bool IsDefault { get; set; }
    [Required]
    [StringLength(2)]
    public string CountryCode { get; set; }
    public bool IsDefaultCountryCalendar { get; set; }
    public bool IsUserCreated { get; set; }
}
