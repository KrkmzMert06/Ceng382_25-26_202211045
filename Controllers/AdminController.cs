using CaterFlow.Data;
using CaterFlow.Models.Entities;
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

            // Revenue
            ViewBag.TotalRevenue = await _context.Orders
                .Where(o => o.Status == OrderStatus.Completed)
                .SumAsync(o => o.TotalPrice);

            // Today's orders
            var todayUtc = DateTime.UtcNow.Date;
            ViewBag.TodayOrders = await _context.Orders
                .CountAsync(o => o.CreatedAt >= todayUtc);

            // Recent orders (last 5)
            ViewBag.RecentOrders = await _context.Orders
                .Include(o => o.AppUser)
                .OrderByDescending(o => o.CreatedAt)
                .Take(5)
                .Select(o => new { o.Id, o.CreatedAt, Customer = o.AppUser.FullName, Status = o.Status.ToString(), o.TotalPrice })
                .ToListAsync();

            return View();
        }

        [HttpGet("/Admin/Report")]
        public async Task<IActionResult> Report(DateTime? from, DateTime? to, string? status, string? search, int page = 1)
        {
            const int pageSize = 20;

            var query = _context.Orders
                .Include(o => o.AppUser)
                .Include(o => o.Items)
                .AsQueryable();

            if (from.HasValue)
                query = query.Where(o => o.CreatedAt >= from.Value.Date.ToUniversalTime());

            if (to.HasValue)
                query = query.Where(o => o.CreatedAt < to.Value.Date.AddDays(1).ToUniversalTime());

            if (!string.IsNullOrWhiteSpace(status) && Enum.TryParse<OrderStatus>(status, out var parsedStatus))
                query = query.Where(o => o.Status == parsedStatus);

            if (!string.IsNullOrWhiteSpace(search))
            {
                query = query.Where(o =>
                    o.Id.ToString().Contains(search) ||
                    o.AppUser.Email.Contains(search) ||
                    o.Items.Any(i => i.MenuItemName.Contains(search)));
            }

            var allMatchingOrders = await query.ToListAsync();
            var totalCount = allMatchingOrders.Count;
            var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);
            page = Math.Clamp(page, 1, Math.Max(totalPages, 1));

            var pagedOrders = allMatchingOrders
                .OrderByDescending(o => o.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(o => new AdminReportOrderViewModel
                {
                    Id = o.Id,
                    CreatedAt = o.CreatedAt,
                    CustomerEmail = o.AppUser.Email,
                    Status = o.Status.ToString(),
                    ItemCount = o.Items.Sum(i => i.Quantity),
                    TotalPrice = o.TotalPrice
                })
                .ToList();

            var topMenuItems = allMatchingOrders
                .SelectMany(o => o.Items)
                .GroupBy(i => i.MenuItemName)
                .Select(g => new AdminReportMenuItemViewModel
                {
                    Name = g.Key,
                    QuantitySold = g.Sum(i => i.Quantity),
                    Revenue = g.Sum(i => i.LineTotal)
                })
                .OrderByDescending(i => i.Revenue)
                .Take(5)
                .ToList();

            var completedOrders = allMatchingOrders.Count(o => o.Status == OrderStatus.Completed);
            var totalRevenue = allMatchingOrders.Sum(o => o.TotalPrice);
            var ratingCount = await _context.Ratings.CountAsync();
            var averageRating = ratingCount == 0 ? 0 : await _context.Ratings.AverageAsync(r => r.Score);

            return View(new AdminReportViewModel
            {
                From = from,
                To = to,
                StatusFilter = status,
                SearchTerm = search,
                CurrentPage = page,
                TotalPages = totalPages,
                PageSize = pageSize,
                TotalOrders = totalCount,
                TotalRevenue = totalRevenue,
                AverageOrderValue = totalCount == 0 ? 0 : totalRevenue / totalCount,
                CompletedOrders = completedOrders,
                TotalItemsSold = allMatchingOrders.SelectMany(o => o.Items).Sum(i => i.Quantity),
                RatingCount = ratingCount,
                AverageMenuRating = averageRating,
                Orders = pagedOrders,
                TopMenuItems = topMenuItems
            });
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
