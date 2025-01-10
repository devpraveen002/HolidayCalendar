namespace HolidayCalendar.src.HolidayCalendar.Core.Entities
{
    public class UserCalendar : BaseEntity
    {
        public long UserId { get; set; } 
        public Guid CalendarId { get; set; }
        public User User { get; set; }
        public Calendar Calendar { get; set; }
        public string CountryCode { get; set; }
    }
}
