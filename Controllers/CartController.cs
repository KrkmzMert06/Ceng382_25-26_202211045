using System.Security.Claims;
using System.Text.Json;
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
    public class CartController : Controller
    {
        private const string CartSessionKey = "CaterFlowCart";
        private readonly ApplicationDbContext _context;
        private readonly LoggingService _loggingService;
        private readonly EmailService _emailService;

        public CartController(ApplicationDbContext context, LoggingService loggingService, EmailService emailService)
        {
            _context = context;
            _loggingService = loggingService;
            _emailService = emailService;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            return View(await BuildCartPageAsync());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Add(AddToCartViewModel model)
        {
            model.Quantity = Math.Clamp(model.Quantity, 1, 99);

            var menuItem = await _context.MenuItems
                .Include(x => x.CustomizationGroups)
                    .ThenInclude(g => g.Options)
                .FirstOrDefaultAsync(x => x.Id == model.MenuItemId && x.IsActive);

            if (menuItem == null)
            {
                TempData["CartMessage"] = "Menu item could not be found.";
                return RedirectToAction("Index", "Home");
            }

            var validOptionIds = menuItem.CustomizationGroups
                .SelectMany(g => g.Options)
                .Select(o => o.Id)
                .ToHashSet();

            var selectedOptionIds = model.SelectedOptionIds
                .Where(validOptionIds.Contains)
                .Distinct()
                .ToList();

            foreach (var group in menuItem.CustomizationGroups)
            {
                var groupOptionIds = group.Options.Select(o => o.Id).ToHashSet();
                var selectedInGroup = selectedOptionIds.Count(groupOptionIds.Contains);

                if (group.IsRequired && selectedInGroup == 0)
                {
                    TempData["CartMessage"] = $"Please choose an option for {group.Name}.";
                    return RedirectToAction("Index", "Home");
                }

                if (!group.AllowsMultipleSelection && selectedInGroup > 1)
                {
                    TempData["CartMessage"] = $"Please choose only one option for {group.Name}.";
                    return RedirectToAction("Index", "Home");
                }
            }

            var cart = GetCart();
            cart.Add(new CartSessionItem
            {
                CartItemId = Guid.NewGuid(),
                MenuItemId = menuItem.Id,
                Quantity = model.Quantity,
                SelectedOptionIds = selectedOptionIds
            });

            SaveCart(cart);
            TempData["CartMessage"] = "Item added to cart.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult UpdateQuantity(Guid cartItemId, int quantity)
        {
            var cart = GetCart();
            var item = cart.FirstOrDefault(x => x.CartItemId == cartItemId);
            if (item != null)
            {
                item.Quantity = Math.Clamp(quantity, 1, 99);
                SaveCart(cart);
            }
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Remove(Guid cartItemId)
        {
            var cart = GetCart();
            cart.RemoveAll(x => x.CartItemId == cartItemId);
            SaveCart(cart);
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> Checkout()
        {
            var cart = await BuildCartPageAsync();
            if (!cart.Items.Any())
            {
                TempData["CartMessage"] = "Your cart is empty.";
                return RedirectToAction(nameof(Index));
            }

            return View(new PaymentViewModel { Cart = cart });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Checkout(PaymentViewModel model)
        {
            var cart = await BuildCartPageAsync();
            model.Cart = cart;

            if (!cart.Items.Any())
                ModelState.AddModelError(string.Empty, "Your cart is empty.");

            if (!ModelState.IsValid)
                return View(model);

            var userIdValue = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(userIdValue, out var userId))
                return Unauthorized();

            var userEmail = User.FindFirstValue(ClaimTypes.Email);
            var userName = User.FindFirstValue(ClaimTypes.Name);

            var order = new Order
            {
                AppUserId = userId,
                TotalPrice = cart.TotalPrice,
                Status = OrderStatus.Completed,
                CreatedAt = DateTime.UtcNow,
                PaidAt = DateTime.UtcNow,
                CardHolderName = model.CardHolderName.Trim(),
                CardLastFourDigits = model.GetCardDigits()[^4..]
            };

            foreach (var cartItem in cart.Items)
            {
                var orderItem = new OrderItem
                {
                    MenuItemId = cartItem.MenuItemId,
                    MenuItemName = cartItem.MenuItemName,
                    UnitPrice = cartItem.UnitPrice,
                    CustomizationTotal = cartItem.CustomizationTotal,
                    Quantity = cartItem.Quantity,
                    LineTotal = cartItem.LineTotal
                };

                foreach (var selected in cartItem.SelectedCustomizations)
                {
                    orderItem.SelectedCustomizations.Add(new OrderItemCustomizationSelection
                    {
                        MenuItemCustomizationOptionId = selected.OptionId,
                        GroupName = selected.GroupName,
                        OptionName = selected.OptionName,
                        PriceModifier = selected.PriceModifier,
                        IsRemovableIngredient = selected.IsRemovableIngredient
                    });
                }

                order.Items.Add(orderItem);
            }

            _context.Orders.Add(order);
            await _context.SaveChangesAsync();
            SaveCart(new List<CartSessionItem>());

            // ── Week 4: Logging ──
            await _loggingService.LogOrderAsync("OrderCreated", userId, userEmail,
                $"Order #{order.Id} created with {order.Items.Count} item(s), total {order.TotalPrice:C}");

            await _loggingService.LogPaymentAsync("PaymentProcessed", userId, userEmail,
                $"Payment for Order #{order.Id}: {order.TotalPrice:C}, card ending {order.CardLastFourDigits}");

            // ── Week 4: Email ──
            var emailItems = order.Items.Select(i => new OrderEmailItem
            {
                Name = i.MenuItemName,
                Quantity = i.Quantity,
                UnitPrice = i.UnitPrice + i.CustomizationTotal,
                LineTotal = i.LineTotal
            }).ToList();

            // Send to user
            if (!string.IsNullOrWhiteSpace(userEmail))
            {
                await _emailService.SendOrderConfirmationToUserAsync(
                    userEmail, userName ?? "Customer", order.Id, order.TotalPrice, emailItems);
            }

            // Send to caterer(s)
            var catererIds = cart.Items.Select(i => i.MenuItemId).Distinct().ToList();
            var menuItemsWithCaterers = await _context.MenuItems
                .Include(m => m.CatererProfile)
                    .ThenInclude(c => c.AppUser)
                .Where(m => catererIds.Contains(m.Id))
                .ToListAsync();

            var caterers = menuItemsWithCaterers
                .Select(m => m.CatererProfile)
                .Where(c => c != null)
                .DistinctBy(c => c.Id)
                .ToList();

            foreach (var caterer in caterers)
            {
                var catererEmail = caterer.AppUser?.Email;
                if (!string.IsNullOrWhiteSpace(catererEmail))
                {
                    var catererItems = order.Items
                        .Where(i => menuItemsWithCaterers.Any(m => m.Id == i.MenuItemId && m.CatererProfileId == caterer.Id))
                        .Select(i => new OrderEmailItem
                        {
                            Name = i.MenuItemName,
                            Quantity = i.Quantity,
                            UnitPrice = i.UnitPrice + i.CustomizationTotal,
                            LineTotal = i.LineTotal
                        }).ToList();

                    await _emailService.SendOrderNotificationToCatererAsync(
                        catererEmail, caterer.BusinessName, userName ?? "Customer",
                        order.Id, order.TotalPrice, catererItems);
                }
            }

            return RedirectToAction(nameof(Success), new { id = order.Id });
        }

        [HttpGet]
        public async Task<IActionResult> Success(int id)
        {
            var details = await LoadOrderDetailsAsync(id);
            if (details == null) return NotFound();
            return View(details);
        }

        [HttpGet]
        public async Task<IActionResult> Orders()
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var orders = await _context.Orders
                .Include(o => o.Items)
                .Where(o => o.AppUserId == userId)
                .OrderByDescending(o => o.CreatedAt)
                .Select(o => new OrderHistoryItemViewModel
                {
                    Id = o.Id,
                    CreatedAt = o.CreatedAt,
                    Status = o.Status.ToString(),
                    TotalPrice = o.TotalPrice,
                    ItemCount = o.Items.Sum(i => i.Quantity)
                })
                .ToListAsync();

            return View(orders);
        }

        [HttpGet]
        public async Task<IActionResult> Details(int id)
        {
            var details = await LoadOrderDetailsAsync(id);
            if (details == null) return NotFound();
            return View(details);
        }

        private async Task<CartPageViewModel> BuildCartPageAsync()
        {
            var cart = GetCart();
            var ids = cart.Select(x => x.MenuItemId).Distinct().ToList();
            var menuItems = await _context.MenuItems
                .Include(x => x.CatererProfile)
                .Include(x => x.CustomizationGroups)
                    .ThenInclude(g => g.Options)
                .Where(x => ids.Contains(x.Id) && x.IsActive)
                .ToListAsync();

            var result = new CartPageViewModel();
            foreach (var sessionItem in cart)
            {
                var menuItem = menuItems.FirstOrDefault(x => x.Id == sessionItem.MenuItemId);
                if (menuItem == null) continue;

                var selectedOptions = menuItem.CustomizationGroups
                    .SelectMany(g => g.Options.Select(o => new { Group = g, Option = o }))
                    .Where(x => sessionItem.SelectedOptionIds.Contains(x.Option.Id))
                    .ToList();

                result.Items.Add(new CartItemViewModel
                {
                    CartItemId = sessionItem.CartItemId,
                    MenuItemId = menuItem.Id,
                    MenuItemName = menuItem.Name,
                    ImageUrl = menuItem.ImageUrl,
                    CatererName = menuItem.CatererProfile.BusinessName,
                    UnitPrice = menuItem.Price,
                    CustomizationTotal = selectedOptions.Sum(x => x.Option.PriceModifier),
                    Quantity = sessionItem.Quantity,
                    SelectedCustomizations = selectedOptions.Select(x => new CartCustomizationViewModel
                    {
                        OptionId = x.Option.Id,
                        GroupName = x.Group.Name,
                        OptionName = x.Option.Name,
                        PriceModifier = x.Option.PriceModifier,
                        IsRemovableIngredient = x.Option.IsRemovableIngredient
                    }).ToList()
                });
            }

            return result;
        }

        private async Task<OrderDetailsViewModel?> LoadOrderDetailsAsync(int orderId)
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var order = await _context.Orders
                .Include(o => o.Items)
                    .ThenInclude(i => i.SelectedCustomizations)
                .FirstOrDefaultAsync(o => o.Id == orderId && o.AppUserId == userId);

            if (order == null) return null;

            // Check if user has rated this order
            var hasRated = await _context.Ratings.AnyAsync(r => r.OrderId == orderId && r.AppUserId == userId);

            return new OrderDetailsViewModel
            {
                Id = order.Id,
                CreatedAt = order.CreatedAt,
                PaidAt = order.PaidAt,
                Status = order.Status.ToString(),
                TotalPrice = order.TotalPrice,
                CardLastFourDigits = order.CardLastFourDigits ?? string.Empty,
                HasBeenRated = hasRated,
                Items = order.Items.Select(i => new CartItemViewModel
                {
                    MenuItemId = i.MenuItemId,
                    MenuItemName = i.MenuItemName,
                    UnitPrice = i.UnitPrice,
                    CustomizationTotal = i.CustomizationTotal,
                    Quantity = i.Quantity,
                    SelectedCustomizations = i.SelectedCustomizations.Select(s => new CartCustomizationViewModel
                    {
                        OptionId = s.MenuItemCustomizationOptionId ?? 0,
                        GroupName = s.GroupName,
                        OptionName = s.OptionName,
                        PriceModifier = s.PriceModifier,
                        IsRemovableIngredient = s.IsRemovableIngredient
                    }).ToList()
                }).ToList()
            };
        }

        private List<CartSessionItem> GetCart()
        {
            var json = HttpContext.Session.GetString(CartSessionKey);
            return string.IsNullOrWhiteSpace(json)
                ? new List<CartSessionItem>()
                : JsonSerializer.Deserialize<List<CartSessionItem>>(json) ?? new List<CartSessionItem>();
        }

        private void SaveCart(List<CartSessionItem> cart)
        {
            HttpContext.Session.SetString(CartSessionKey, JsonSerializer.Serialize(cart));
        }

        private class CartSessionItem
        {
            public Guid CartItemId { get; set; }
            public int MenuItemId { get; set; }
            public int Quantity { get; set; }
            public List<int> SelectedOptionIds { get; set; } = new();
        }
    }
}
