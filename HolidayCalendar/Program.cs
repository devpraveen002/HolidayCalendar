using HolidayCalendar.src.HolidayCalendar.Core.Entities;
using HolidayCalendar.src.HolidayCalendar.Core.Interfaces;
using HolidayCalendar.src.HolidayCalendar.Core.Services;
using HolidayCalendar.src.HolidayCalendar.Infrastructure.Data;
using HolidayCalendar.src.HolidayCalendar.Infrastructure.Repositories;
using HolidayCalendar.src.HolidayCalendar.Infrastructure.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.EntityFrameworkCore;
using OfficeOpenXml;
using Serilog;
using ILogger = Microsoft.Extensions.Logging.ILogger;

namespace HolidayCalendar
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Configure services
            builder.Services.AddDbContext<ApplicationDbContext>(options =>
                options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection"))
                       .EnableSensitiveDataLogging()
                       .EnableDetailedErrors());

            builder.Services.AddIdentity<User, IdentityRole<long>>(options =>
            {
                options.Password.RequireDigit = true;
                options.Password.RequiredLength = 8;
                options.Password.RequireNonAlphanumeric = true;
                options.Password.RequireUppercase = true;
                options.Password.RequireLowercase = true;
            })
            .AddEntityFrameworkStores<ApplicationDbContext>()
            .AddDefaultTokenProviders();

            // Configure logging
            builder.Logging.ClearProviders();
            builder.Host.UseSerilog(new LoggerConfiguration()
                .ReadFrom.Configuration(builder.Configuration)
                .WriteTo.File("logs/holiday-calendar-.txt", rollingInterval: RollingInterval.Day)
                .CreateLogger());

            // Add services to the container
            builder.Services.AddScoped<ICalendarRepository, CalendarRepository>();
            builder.Services.AddScoped<IHolidayRepository, HolidayRepository>();
            builder.Services.AddScoped<ICalendarService, CalendarService>();
            builder.Services.AddScoped<ICalendarHolidayRepository, CalendarHolidayRepository>();

            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

            builder.Services.AddControllersWithViews();
            builder.Services.ConfigureApplicationCookie(options =>
            {
                options.LoginPath = "/Account/Login";
                options.LogoutPath = "/Account/Logout";
                options.AccessDeniedPath = "/Account/AccessDenied";
            });

            builder.Services.AddAuthorization(options =>
            {
                options.AddPolicy("AdminOnly", policy => policy.RequireRole("Admin"));
            });

            var app = builder.Build();

            using var scope = app.Services.CreateScope();
            var services = scope.ServiceProvider;
            var logger = services.GetRequiredService<ILogger<Program>>();

            try
            {
                logger.LogInformation("Starting database initialization");
                var context = services.GetRequiredService<ApplicationDbContext>();

                if (!await context.Database.CanConnectAsync())
                {
                    logger.LogError("Unable to connect to the database. Please check the connection string.");
                    return;
                }

                await ApplyMigrationsAsync(context, logger);
                await DbInitializer.Initialize(context, services);
                logger.LogInformation("Database initialization completed successfully.");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "An error occurred during database initialization. The application will not start.");
                return;
            }

            // Configure middleware
            if (app.Environment.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseExceptionHandler("/Home/Error");
                app.UseHsts();
            }

            app.UseHttpsRedirection();
            app.UseStaticFiles(new StaticFileOptions
            {
                ContentTypeProvider = new FileExtensionContentTypeProvider
                {
                    Mappings = { [".pdf"] = "application/pdf", [".ics"] = "text/calendar" }
                }
            });

            app.UseRouting();
            app.UseAuthentication();
            app.UseAuthorization();

            app.MapControllerRoute(
                name: "default",
                pattern: "{controller=Calendar}/{action=Index}/{id?}");

            app.Run();
        }

        private static async Task ApplyMigrationsAsync(ApplicationDbContext context, ILogger logger)
        {
            if (context.Database.GetPendingMigrations().Any())
            {
                logger.LogWarning("Applying pending migrations...");
                await context.Database.MigrateAsync();
                logger.LogInformation("Applied pending migrations successfully.");
            }
            else
            {
                logger.LogInformation("No pending migrations found.");
            }
        }
    }
}
