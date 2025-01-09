// Updated DbInitializer.cs
using HolidayCalendar.src.HolidayCalendar.Core.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace HolidayCalendar.src.HolidayCalendar.Infrastructure.Data;

public static class DbInitializer
{
    public static async Task Initialize(ApplicationDbContext context, IServiceProvider services)
    {
        var logger = services.GetRequiredService<ILogger<ApplicationDbContext>>();
        logger.LogInformation("Starting database initialization");

        try
        {
            // Ensure roles are created
            await EnsureRolesAsync(services, logger);

            // Create the admin user
            var adminUser = await EnsureAdminUserAsync(services, logger);

            // Create default countries and their calendars
            await EnsureDefaultCountriesAndCalendarsAsync(context, adminUser.Id, logger);

            logger.LogInformation("Database initialization completed successfully");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "An error occurred during database initialization");
            throw;
        }
    }

    private static async Task EnsureRolesAsync(IServiceProvider services, ILogger logger)
    {
        var roleManager = services.GetRequiredService<RoleManager<IdentityRole<long>>>();
        string[] roles = { "Admin", "User" };

        foreach (var role in roles)
        {
            if (!await roleManager.RoleExistsAsync(role))
            {
                await roleManager.CreateAsync(new IdentityRole<long>(role));
                logger.LogInformation("Created role: {Role}", role);
            }
        }
    }

    private static async Task<User> EnsureAdminUserAsync(IServiceProvider services, ILogger logger)
    {
        var userManager = services.GetRequiredService<UserManager<User>>();
        var adminEmail = "admin@example.com";
        var adminUser = await userManager.FindByEmailAsync(adminEmail);

        if (adminUser == null)
        {
            adminUser = new User
            {
                UserName = adminEmail,
                Email = adminEmail,
                EmailConfirmed = true,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = 1
            };

            var result = await userManager.CreateAsync(adminUser, "Admin@123");
            if (result.Succeeded)
            {
                await userManager.AddToRoleAsync(adminUser, "Admin");
                logger.LogInformation("Created admin user");
            }
            else
            {
                throw new Exception("Failed to create admin user: " +
                    string.Join(", ", result.Errors.Select(e => e.Description)));
            }
        }
        else
        {
            logger.LogInformation("Admin user already exists");
        }

        return adminUser;
    }

    private static async Task EnsureDefaultCountriesAndCalendarsAsync(
        ApplicationDbContext context,
        long adminUserId,
        ILogger logger)
    {
        var defaultCountries = new[]
        {
            new { Code = "US", Name = "United States" },
            new { Code = "CA", Name = "Canada" },
            new { Code = "UK", Name = "United Kingdom" },
            new { Code = "AU", Name = "Australia" }
        };

        foreach (var country in defaultCountries)
        {
            if (!await context.Countries.AnyAsync(c => c.CountryCode == country.Code))
            {
                context.Countries.Add(new Country
                {
                    Id = Guid.NewGuid(),
                    CountryCode = country.Code,
                    CountryName = country.Name,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow,
                    CreatedBy = adminUserId
                });
            }
        }
        await context.SaveChangesAsync();

        foreach (var country in defaultCountries)
        {
            if (!await context.Calendars.AnyAsync(c => c.IsDefault && c.CountryCode == country.Code))
            {
                using var transaction = await context.Database.BeginTransactionAsync();
                try
                {
                    var calendar = new Calendar
                    {
                        Id = Guid.NewGuid(),
                        Name = $"{country.Name} Default Calendar",
                        IsDefault = true,
                        CountryCode = country.Code,
                        ShareableLink = Guid.NewGuid().ToString(),
                        CreatedAt = DateTime.UtcNow,
                        CreatedBy = adminUserId
                    };

                    context.Calendars.Add(calendar);
                    await context.SaveChangesAsync();

                    var holidays = CreateDefaultHolidays(country.Code, adminUserId);
                    await context.Holidays.AddRangeAsync(holidays);
                    await context.SaveChangesAsync();

                    var calendarHolidays = holidays.Select(h => new CalendarHoliday
                    {
                        Id = Guid.NewGuid(),
                        CalendarId = calendar.Id,
                        HolidayId = h.Id,
                        CreatedAt = DateTime.UtcNow,
                        CreatedBy = adminUserId
                    });

                    await context.CalendarHolidays.AddRangeAsync(calendarHolidays);
                    await context.SaveChangesAsync();

                    await transaction.CommitAsync();
                    logger.LogInformation("Created default calendar and holidays for {Country}", country.Name);
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    logger.LogError(ex, "Error creating default calendar for {Country}", country.Name);
                    throw;
                }
            }
        }
    }

    private static List<Holiday> CreateDefaultHolidays(string countryCode, long createdBy)
    {
        var currentYear = DateTime.UtcNow.Year;
        var holidays = new List<Holiday>();

        switch (countryCode)
        {
            case "US":
                holidays.AddRange(new[]
                {
                    CreateHoliday("New Year's Day", new DateTime(currentYear, 1, 1), true, true, createdBy),
                    CreateHoliday("Martin Luther King Jr. Day", new DateTime(currentYear, 1, 15), true, true, createdBy),
                    CreateHoliday("Presidents' Day", new DateTime(currentYear, 2, 19), true, true, createdBy),
                    CreateHoliday("Memorial Day", new DateTime(currentYear, 5, 27), true, true, createdBy),
                    CreateHoliday("Independence Day", new DateTime(currentYear, 7, 4), true, true, createdBy),
                    CreateHoliday("Labor Day", new DateTime(currentYear, 9, 2), true, true, createdBy),
                    CreateHoliday("Columbus Day", new DateTime(currentYear, 10, 14), true, true, createdBy),
                    CreateHoliday("Veterans Day", new DateTime(currentYear, 11, 11), true, true, createdBy),
                    CreateHoliday("Thanksgiving Day", new DateTime(currentYear, 11, 28), true, true, createdBy),
                    CreateHoliday("Christmas Day", new DateTime(currentYear, 12, 25), true, true, createdBy)
                });
                break;

            case "CA":
                holidays.AddRange(new[]
                {
                    CreateHoliday("New Year's Day", new DateTime(currentYear, 1, 1), true, true, createdBy),
                    CreateHoliday("Family Day", new DateTime(currentYear, 2, 19), true, true, createdBy),
                    CreateHoliday("Good Friday", new DateTime(currentYear, 3, 29), true, true, createdBy),
                    CreateHoliday("Victoria Day", new DateTime(currentYear, 5, 20), true, true, createdBy),
                    CreateHoliday("Canada Day", new DateTime(currentYear, 7, 1), true, true, createdBy),
                    CreateHoliday("Labour Day", new DateTime(currentYear, 9, 2), true, true, createdBy),
                    CreateHoliday("Thanksgiving Day", new DateTime(currentYear, 10, 14), true, true, createdBy),
                    CreateHoliday("Remembrance Day", new DateTime(currentYear, 11, 11), true, true, createdBy),
                    CreateHoliday("Christmas Day", new DateTime(currentYear, 12, 25), true, true, createdBy),
                    CreateHoliday("Boxing Day", new DateTime(currentYear, 12, 26), true, true, createdBy)
                });
                break;

            case "UK":
                holidays.AddRange(new[]
                {
                    CreateHoliday("New Year's Day", new DateTime(currentYear, 1, 1), true, true, createdBy),
                    CreateHoliday("Good Friday", new DateTime(currentYear, 3, 29), true, true, createdBy),
                    CreateHoliday("Easter Monday", new DateTime(currentYear, 4, 1), true, true, createdBy),
                    CreateHoliday("Early May Bank Holiday", new DateTime(currentYear, 5, 6), true, true, createdBy),
                    CreateHoliday("Spring Bank Holiday", new DateTime(currentYear, 5, 27), true, true, createdBy),
                    CreateHoliday("Summer Bank Holiday", new DateTime(currentYear, 8, 26), true, true, createdBy),
                    CreateHoliday("Christmas Day", new DateTime(currentYear, 12, 25), true, true, createdBy),
                    CreateHoliday("Boxing Day", new DateTime(currentYear, 12, 26), true, true, createdBy)
                });
                break;

            case "AU":
                holidays.AddRange(new[]
                {
                    CreateHoliday("New Year's Day", new DateTime(currentYear, 1, 1), true, true, createdBy),
                    CreateHoliday("Australia Day", new DateTime(currentYear, 1, 26), true, true, createdBy),
                    CreateHoliday("Good Friday", new DateTime(currentYear, 3, 29), true, true, createdBy),
                    CreateHoliday("Easter Monday", new DateTime(currentYear, 4, 1), true, true, createdBy),
                    CreateHoliday("Anzac Day", new DateTime(currentYear, 4, 25), true, true, createdBy),
                    CreateHoliday("Christmas Day", new DateTime(currentYear, 12, 25), true, true, createdBy),
                    CreateHoliday("Boxing Day", new DateTime(currentYear, 12, 26), true, true, createdBy)
                });
                break;
        }

        return holidays;
    }

    private static Holiday CreateHoliday(string name, DateTime date, bool isFixed, bool isWeekendAdjustable, long createdBy)
    {
        return new Holiday
        {
            Id = Guid.NewGuid(),
            Name = name,
            Date = DateTime.SpecifyKind(date, DateTimeKind.Utc),
            IsFixedHoliday = isFixed,
            IsWeekendAdjustable = isWeekendAdjustable,
            IsDefault = true,
            Description = $"Official {name} holiday",
            CreatedAt = DateTime.UtcNow,
            CreatedBy = createdBy
        };
    }
}
