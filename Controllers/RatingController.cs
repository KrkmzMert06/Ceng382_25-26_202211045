using System.Security.Claims;
using CaterFlow.Data;
using CaterFlow.Models.Entities;
using CaterFlow.Models.ViewModels;
using CaterFlow.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CaterFlow.Controllers
{
    [Authorize(Roles = "User")]
    public class RatingController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly LoggingService _loggingService;

        public RatingController(ApplicationDbContext context, LoggingService loggingService)
        {
            _context = context;
            _loggingService = loggingService;
        }

        [HttpGet]
        public async Task<IActionResult> Rate(int orderId)
        {
            var userId = GetUserId();
            var order = await _context.Orders
                .Include(o => o.Items)
                    .ThenInclude(i => i.MenuItem)
                        .ThenInclude(m => m.CatererProfile)
                .FirstOrDefaultAsync(o => o.Id == orderId && o.AppUserId == userId && o.Status == OrderStatus.Completed);

            if (order == null) return NotFound();

            var existingRatings = await _context.Ratings
                .Where(r => r.OrderId == orderId && r.AppUserId == userId)
                .ToListAsync();

            var catererProfile = order.Items.FirstOrDefault()?.MenuItem?.CatererProfile;

            var model = new RateOrderViewModel
            {
                OrderId = orderId,
                MenuItems = order.Items
                    .GroupBy(i => i.MenuItemId)
                    .Select(g =>
                    {
                        var item = g.First();
                        var existing = existingRatings.FirstOrDefault(r => r.MenuItemId == item.MenuItemId);
                        return new RateMenuItemRow
                        {
                            MenuItemId = item.MenuItemId,
                            MenuItemName = item.MenuItemName,
                            ImageUrl = item.MenuItem?.ImageUrl ?? "",
                            AlreadyRated = existing != null,
                            ExistingScore = existing?.Score,
                            ExistingComment = existing?.Comment,
                            Score = existing?.Score ?? 0
                        };
                    })
                    .ToList()
            };

            if (catererProfile != null)
            {
                var existingCatererRating = existingRatings.FirstOrDefault(r => r.CatererProfileId == catererProfile.Id);
                model.Caterer = new RateCatererRow
                {
                    CatererProfileId = catererProfile.Id,
                    CatererName = catererProfile.BusinessName,
                    AlreadyRated = existingCatererRating != null,
                    ExistingScore = existingCatererRating?.Score,
                    ExistingComment = existingCatererRating?.Comment,
                    Score = existingCatererRating?.Score ?? 0
                };
            }

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Rate(RateOrderViewModel model)
        {
            var userId = GetUserId();
            var userEmail = User.FindFirstValue(ClaimTypes.Email);

            var order = await _context.Orders
                .Include(o => o.Items)
                    .ThenInclude(i => i.MenuItem)
                .FirstOrDefaultAsync(o => o.Id == model.OrderId && o.AppUserId == userId && o.Status == OrderStatus.Completed);

            if (order == null) return NotFound();

            // Save menu item ratings
            foreach (var menuItemRow in model.MenuItems.Where(r => r.Score >= 1 && r.Score <= 5 && !r.AlreadyRated))
            {
                var orderItem = order.Items.FirstOrDefault(i => i.MenuItemId == menuItemRow.MenuItemId);
                if (orderItem == null) continue;

                _context.Ratings.Add(new Rating
                {
                    OrderId = model.OrderId,
                    AppUserId = userId,
                    MenuItemId = menuItemRow.MenuItemId,
                    Score = menuItemRow.Score,
                    Comment = menuItemRow.Comment?.Trim(),
                    CreatedAt = DateTime.UtcNow
                });

                // Update average rating for menu item
                var menuItem = orderItem.MenuItem;
                if (menuItem != null)
                {
                    var allRatings = await _context.Ratings
                        .Where(r => r.MenuItemId == menuItemRow.MenuItemId)
                        .Select(r => r.Score)
                        .ToListAsync();
                    allRatings.Add(menuItemRow.Score);
                    menuItem.AverageRating = allRatings.Average();
                }
            }

            // Save caterer rating
            if (model.Caterer != null && model.Caterer.Score >= 1 && model.Caterer.Score <= 5 && !model.Caterer.AlreadyRated)
            {
                _context.Ratings.Add(new Rating
                {
                    OrderId = model.OrderId,
                    AppUserId = userId,
                    CatererProfileId = model.Caterer.CatererProfileId,
                    Score = model.Caterer.Score,
                    Comment = model.Caterer.Comment?.Trim(),
                    CreatedAt = DateTime.UtcNow
                });

                // Update average rating for caterer
                var caterer = await _context.CatererProfiles.FindAsync(model.Caterer.CatererProfileId);
                if (caterer != null)
                {
                    var allCatererRatings = await _context.Ratings
                        .Where(r => r.CatererProfileId == model.Caterer.CatererProfileId)
                        .Select(r => r.Score)
                        .ToListAsync();
                    allCatererRatings.Add(model.Caterer.Score);
                    caterer.AverageRating = allCatererRatings.Average();
                }
            }

            await _context.SaveChangesAsync();

            await _loggingService.LogRatingAsync("RatingSubmitted", userId, userEmail,
                $"Order #{model.OrderId} rated.");

            TempData["Success"] = "Thank you for your ratings!";
            return RedirectToAction("Details", "Cart", new { id = model.OrderId });
        }

        private int GetUserId()
        {
            return int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        }
    }
}
