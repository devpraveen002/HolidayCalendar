namespace HolidayCalendar.src.HolidayCalendar.Core.Entities
{
    public class UserCalendar : BaseEntity
    {
        public long UserId { get; set; }  // Changed to long
        public Guid CalendarId { get; set; }
        public User User { get; set; }
        public Calendar Calendar { get; set; }
        //public int Id { get; set; }
        //public int UserId { get; set; }
        //public int CalendarId { get; set; }
        //public int CreatedBy { get; set; }
        //public DateTime CreatedAt { get; set; }
        //public int? ModifiedBy { get; set; }
        //public DateTime? ModifiedAt { get; set; }
    }
}
