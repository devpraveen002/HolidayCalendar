using HolidayCalendar.src.HolidayCalendar.Core.Entities;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore;

namespace HolidayCalendar.src.HolidayCalendar.Infrastructure.Data.Configrations;

public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.HasMany(u => u.Calendars)
               .WithOne(c => c.User)
               .HasForeignKey(c => c.UserId)
               .OnDelete(DeleteBehavior.Cascade);
    }
}
