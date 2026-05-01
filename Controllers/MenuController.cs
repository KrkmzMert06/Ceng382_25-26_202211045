using System.Globalization;
using System.Security.Claims;
using CaterFlow.Data;
using CaterFlow.Models.Entities;
using CaterFlow.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CaterFlow.Controllers
{
    [Authorize(Roles = "Caterer")]
    public class MenuController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _environment;

        public MenuController(ApplicationDbContext context, IWebHostEnvironment environment)
        {
            _context = context;
            _environment = environment;
        }

        public async Task<IActionResult> MyItems()
        {
            var caterer = await GetCurrentCatererAsync();
            if (caterer == null)
                return Forbid();

            var items = await _context.MenuItems
                .Include(x => x.CustomizationGroups)
                    .ThenInclude(g => g.Options)
                .Where(x => x.CatererProfileId == caterer.Id)
                .OrderByDescending(x => x.Id)
                .ToListAsync();

            return View(items);
        }

        [HttpGet]
        public IActionResult Create()
        {
            return View(new MenuCreateViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(MenuCreateViewModel model)
        {
            ValidateImage(model.ImageFile, required: true);

            if (!ModelState.IsValid)
                return View(model);

            var caterer = await GetCurrentCatererAsync();
            if (caterer == null)
                return Forbid();

            var imageUrl = await SaveImageAsync(model.ImageFile!);
            var menuItem = new MenuItem
            {
                CatererProfileId = caterer.Id,
                Name = model.Name.Trim(),
                Description = model.Description?.Trim(),
                Price = model.Price,
                ImageUrl = imageUrl,
                IsActive = true
            };

            ApplyCustomizationText(menuItem, model.RemovableIngredientsText, model.CustomizationGroupsText);

            _context.MenuItems.Add(menuItem);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Menu item created successfully.";
            return RedirectToAction(nameof(MyItems));
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var menuItem = await GetOwnedMenuItemAsync(id);
            if (menuItem == null)
                return NotFound();

            return View(ToEditViewModel(menuItem));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(MenuEditViewModel model)
        {
            ValidateImage(model.ImageFile, required: false);

            var menuItem = await GetOwnedMenuItemAsync(model.Id);
            if (menuItem == null)
                return NotFound();

            if (!ModelState.IsValid)
            {
                if (string.IsNullOrWhiteSpace(model.ExistingImageUrl))
                    model.ExistingImageUrl = menuItem.ImageUrl;
                return View(model);
            }

            menuItem.Name = model.Name.Trim();
            menuItem.Description = model.Description?.Trim();
            menuItem.Price = model.Price;
            menuItem.IsActive = model.IsActive;

            if (model.ImageFile != null)
                menuItem.ImageUrl = await SaveImageAsync(model.ImageFile);

            menuItem.CustomizationGroups.Clear();
            ApplyCustomizationText(menuItem, model.RemovableIngredientsText, model.CustomizationGroupsText);

            await _context.SaveChangesAsync();

            TempData["Success"] = "Menu item updated successfully.";
            return RedirectToAction(nameof(MyItems));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var menuItem = await GetOwnedMenuItemAsync(id);
            if (menuItem == null)
                return NotFound();

            menuItem.IsActive = false;
            await _context.SaveChangesAsync();

            TempData["Success"] = "Menu item hidden from users.";
            return RedirectToAction(nameof(MyItems));
        }

        private async Task<CatererProfile?> GetCurrentCatererAsync()
        {
            int userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            return await _context.CatererProfiles.FirstOrDefaultAsync(x => x.AppUserId == userId);
        }

        private async Task<MenuItem?> GetOwnedMenuItemAsync(int id)
        {
            var caterer = await GetCurrentCatererAsync();
            if (caterer == null)
                return null;

            return await _context.MenuItems
                .Include(x => x.CustomizationGroups)
                    .ThenInclude(g => g.Options)
                .FirstOrDefaultAsync(x => x.Id == id && x.CatererProfileId == caterer.Id);
        }

        private static MenuEditViewModel ToEditViewModel(MenuItem item)
        {
            return new MenuEditViewModel
            {
                Id = item.Id,
                Name = item.Name,
                Description = item.Description,
                Price = item.Price,
                ExistingImageUrl = item.ImageUrl,
                IsActive = item.IsActive,
                RemovableIngredientsText = string.Join(Environment.NewLine, item.CustomizationGroups
                    .Where(g => g.Name == "Removable Ingredients")
                    .SelectMany(g => g.Options)
                    .OrderBy(o => o.DisplayOrder)
                    .Select(o => o.Name)),
                CustomizationGroupsText = BuildCustomizationText(item)
            };
        }

        private static string BuildCustomizationText(MenuItem item)
        {
            var blocks = item.CustomizationGroups
                .Where(g => g.Name != "Removable Ingredients")
                .OrderBy(g => g.DisplayOrder)
                .Select(g => $"Group: {g.Name}{Environment.NewLine}" + string.Join(Environment.NewLine, g.Options
                    .OrderBy(o => o.DisplayOrder)
                    .Select(o => o.PriceModifier == 0 ? o.Name : $"{o.Name}|{o.PriceModifier.ToString(CultureInfo.InvariantCulture)}")));

            return string.Join(Environment.NewLine + Environment.NewLine, blocks);
        }

        private static void ApplyCustomizationText(MenuItem menuItem, string? removableText, string? groupsText)
        {
            var displayOrder = 0;
            var removableIngredients = SplitLines(removableText).ToList();
            if (removableIngredients.Any())
            {
                var group = new MenuItemCustomizationGroup
                {
                    Name = "Removable Ingredients",
                    AllowsMultipleSelection = true,
                    IsRequired = false,
                    DisplayOrder = displayOrder++
                };

                var optionOrder = 0;
                foreach (var ingredient in removableIngredients)
                {
                    group.Options.Add(new MenuItemCustomizationOption
                    {
                        Name = ingredient,
                        PriceModifier = 0,
                        IsRemovableIngredient = true,
                        IsDefaultSelected = true,
                        DisplayOrder = optionOrder++
                    });
                }

                menuItem.CustomizationGroups.Add(group);
            }

            MenuItemCustomizationGroup? currentGroup = null;
            foreach (var rawLine in SplitLines(groupsText))
            {
                if (rawLine.StartsWith("Group:", StringComparison.OrdinalIgnoreCase))
                {
                    var groupName = rawLine.Substring("Group:".Length).Trim();
                    if (string.IsNullOrWhiteSpace(groupName))
                        continue;

                    currentGroup = new MenuItemCustomizationGroup
                    {
                        Name = groupName,
                        AllowsMultipleSelection = true,
                        IsRequired = false,
                        DisplayOrder = displayOrder++
                    };
                    menuItem.CustomizationGroups.Add(currentGroup);
                    continue;
                }

                if (currentGroup == null)
                {
                    currentGroup = new MenuItemCustomizationGroup
                    {
                        Name = "Extra Additions",
                        AllowsMultipleSelection = true,
                        IsRequired = false,
                        DisplayOrder = displayOrder++
                    };
                    menuItem.CustomizationGroups.Add(currentGroup);
                }

                var parts = rawLine.Split('|', StringSplitOptions.TrimEntries);
                var optionName = parts[0].Trim();
                if (string.IsNullOrWhiteSpace(optionName))
                    continue;

                decimal priceModifier = 0;
                if (parts.Length > 1)
                    decimal.TryParse(parts[1], NumberStyles.Number, CultureInfo.InvariantCulture, out priceModifier);

                currentGroup.Options.Add(new MenuItemCustomizationOption
                {
                    Name = optionName,
                    PriceModifier = priceModifier,
                    IsRemovableIngredient = false,
                    IsDefaultSelected = false,
                    DisplayOrder = currentGroup.Options.Count
                });
            }
        }

        private static IEnumerable<string> SplitLines(string? text)
        {
            return (text ?? string.Empty)
                .Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries)
                .Select(x => x.Trim())
                .Where(x => !string.IsNullOrWhiteSpace(x));
        }

        private void ValidateImage(IFormFile? imageFile, bool required)
        {
            if (imageFile == null)
            {
                if (required)
                    ModelState.AddModelError(nameof(MenuCreateViewModel.ImageFile), "Menu image is required.");
                return;
            }

            var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".webp", ".gif" };
            var extension = Path.GetExtension(imageFile.FileName).ToLowerInvariant();
            if (!allowedExtensions.Contains(extension))
                ModelState.AddModelError("ImageFile", "Only JPG, PNG, WEBP or GIF images are allowed.");

            if (imageFile.Length > 5 * 1024 * 1024)
                ModelState.AddModelError("ImageFile", "Image size must be 5 MB or smaller.");
        }

        private async Task<string> SaveImageAsync(IFormFile imageFile)
        {
            var uploadDirectory = Path.Combine(_environment.WebRootPath, "uploads", "menu-items");
            Directory.CreateDirectory(uploadDirectory);

            var extension = Path.GetExtension(imageFile.FileName).ToLowerInvariant();
            var fileName = $"{Guid.NewGuid():N}{extension}";
            var path = Path.Combine(uploadDirectory, fileName);

            await using var stream = new FileStream(path, FileMode.Create);
            await imageFile.CopyToAsync(stream);

            return $"/uploads/menu-items/{fileName}";
        }
    }
}
