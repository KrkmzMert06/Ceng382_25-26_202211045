using CaterFlow.Data;
using CaterFlow.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CaterFlow.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {
        private readonly ApplicationDbContext _context;

        public AdminController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            ViewBag.UserCount = await _context.AppUsers.CountAsync();
            ViewBag.CatererCount = await _context.CatererProfiles.CountAsync();
            ViewBag.MenuCount = await _context.MenuItems.CountAsync();
            ViewBag.OrderCount = await _context.Orders.CountAsync();
            ViewBag.RatingCount = await _context.Ratings.CountAsync();
            ViewBag.LogCount = await _context.SystemLogs.CountAsync();
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> Logs(string? eventType, string? level, string? search, int page = 1)
        {
            const int pageSize = 25;

            var query = _context.SystemLogs.AsQueryable();

            if (!string.IsNullOrWhiteSpace(eventType))
                query = query.Where(l => l.EventType == eventType);

            if (!string.IsNullOrWhiteSpace(level))
                query = query.Where(l => l.Level == level);

            if (!string.IsNullOrWhiteSpace(search))
                query = query.Where(l =>
                    (l.Action != null && l.Action.Contains(search)) ||
                    (l.Details != null && l.Details.Contains(search)) ||
                    (l.UserEmail != null && l.UserEmail.Contains(search)));

            var totalCount = await query.CountAsync();
            var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);
            page = Math.Clamp(page, 1, Math.Max(totalPages, 1));

            var logs = await query
                .OrderByDescending(l => l.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(l => new LogItemViewModel
                {
                    Id = l.Id,
                    EventType = l.EventType,
                    Action = l.Action,
                    Details = l.Details,
                    UserEmail = l.UserEmail,
                    Level = l.Level,
                    IpAddress = l.IpAddress,
                    CreatedAt = l.CreatedAt
                })
                .ToListAsync();

            var model = new LogListViewModel
            {
                Logs = logs,
                FilterEventType = eventType,
                FilterLevel = level,
                SearchTerm = search,
                CurrentPage = page,
                TotalPages = totalPages,
                PageSize = pageSize
            };

            return View(model);
        }
    }
}
