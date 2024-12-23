using HolidayCalendar.src.HolidayCalendar.Core.Interfaces;
using HolidayCalendar.src.HolidayCalendar.Infrastructure.Data;

namespace HolidayCalendar.src.HolidayCalendar.Infrastructure.Repositories
{
    public class HolidayRepository : IHolidayRepository
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<HolidayRepository> _logger;

        public HolidayRepository(ApplicationDbContext context, ILogger<HolidayRepository> logger)
        {
            _context = context;
            _logger = logger;
        }

        // Implement IHolidayRepository methods
    }
}
