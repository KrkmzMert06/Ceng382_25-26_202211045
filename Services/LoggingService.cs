using CaterFlow.Data;
using CaterFlow.Models.Entities;

namespace CaterFlow.Services
{
    public class LoggingService
    {
        private readonly ApplicationDbContext _context;

        public LoggingService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task LogAsync(string eventType, string action, string? details = null,
            int? userId = null, string? userEmail = null, string? ipAddress = null, string level = "Info")
        {
            var log = new SystemLog
            {
                EventType = eventType,
                Action = action,
                Details = details?.Length > 500 ? details[..500] : details,
                AppUserId = userId,
                UserEmail = userEmail?.Length > 150 ? userEmail[..150] : userEmail,
                IpAddress = ipAddress?.Length > 50 ? ipAddress[..50] : ipAddress,
                Level = level,
                CreatedAt = DateTime.UtcNow
            };

            _context.SystemLogs.Add(log);
            await _context.SaveChangesAsync();
        }

        public async Task LogAuthAsync(string action, int? userId, string? email, string? ip, string? details = null)
            => await LogAsync("Auth", action, details, userId, email, ip);

        public async Task LogOrderAsync(string action, int? userId, string? email, string? details = null)
            => await LogAsync("Order", action, details, userId, email);

        public async Task LogPaymentAsync(string action, int? userId, string? email, string? details = null)
            => await LogAsync("Payment", action, details, userId, email);

        public async Task LogRatingAsync(string action, int? userId, string? email, string? details = null)
            => await LogAsync("Rating", action, details, userId, email);

        public async Task LogErrorAsync(string action, string? details = null, int? userId = null, string? email = null)
            => await LogAsync("Error", action, details, userId, email, level: "Error");

        public async Task LogSystemAsync(string action, string? details = null)
            => await LogAsync("System", action, details, level: "Info");
    }
}
