using HolidayCalendar.src.HolidayCalendar.Core.Entities;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore;

namespace HolidayCalendar.src.HolidayCalendar.Infrastructure.Data.Configrations;

public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        // Configure primary key
        builder.HasKey(u => u.Id);

        // Configure Identity properties
        builder.Property(u => u.UserName).HasMaxLength(256).IsRequired();
        builder.Property(u => u.Email).HasMaxLength(256).IsRequired();
        builder.Property(u => u.NormalizedUserName).HasMaxLength(256);
        builder.Property(u => u.NormalizedEmail).HasMaxLength(256);

        // Configure audit properties
        builder.Property(u => u.CreatedAt).IsRequired();
        builder.Property(u => u.CreatedBy).IsRequired();
        builder.Property(u => u.ModifiedAt);
        builder.Property(u => u.ModifiedBy);

        // Configure indexes
        builder.HasIndex(u => u.NormalizedUserName).HasDatabaseName("UserNameIndex").IsUnique();
        builder.HasIndex(u => u.NormalizedEmail).HasDatabaseName("EmailIndex");

        // Configure the relationship with UserCalendar
        builder.HasMany<UserCalendar>()
              .WithOne(uc => uc.User)
              .HasForeignKey(uc => uc.UserId)
              .OnDelete(DeleteBehavior.Restrict);

        // Table name configuration (if needed)
        builder.ToTable("Users");
    }
}
