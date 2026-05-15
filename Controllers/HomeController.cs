using System.Diagnostics;
using System.Security.Claims;
using CaterFlow.Data;
using CaterFlow.Models;
using CaterFlow.Models.Entities;
using CaterFlow.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CaterFlow.Controllers;

public class HomeController : Controller
{
    private readonly ApplicationDbContext _context;

    public HomeController(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> Index(double? lat, double? lng, double? maxDistance)
    {
        var (userLat, userLng, maxDist) = await ResolveLocationAsync(lat, lng, maxDistance);

        var caterers = await _context.CatererProfiles
            .Include(c => c.AppUser)
            .ToListAsync();

        var activeMenuItems = await _context.MenuItems
            .Where(m => m.IsActive)
            .ToListAsync();

        var restaurants = caterers
            .Select(c =>
            {
                var menuItems = activeMenuItems.Where(m => m.CatererProfileId == c.Id).ToList();
                var distanceKm = CalculateDistanceForCaterer(userLat, userLng, c);
                var coverImage = GetRestaurantCoverImage(c.BusinessName);

                return new NearbyRestaurantViewModel
                {
                    Id = c.Id,
                    BusinessName = c.BusinessName,
                    Description = c.Description,
                    Address = c.Address ?? c.AppUser.Address,
                    CoverImageUrl = coverImage,
                    DistanceKm = distanceKm,
                    AverageRating = c.AverageRating,
                    MenuItemCount = menuItems.Count,
                    StartingPrice = menuItems.Any() ? menuItems.Min(m => m.Price) : 0,
                    CatererLat = c.Latitude ?? 0,
                    CatererLng = c.Longitude ?? 0
                };
            })
            .Where(r => r.MenuItemCount > 0)
            .ToList();

        if (userLat.HasValue && userLng.HasValue)
        {
            restaurants = restaurants
                .Where(r => r.CatererLat != 0 && r.CatererLng != 0 && r.DistanceKm <= maxDist)
                .OrderBy(r => r.DistanceKm)
                .ThenByDescending(r => r.AverageRating)
                .ToList();
        }
        else
        {
            restaurants = restaurants
                .OrderByDescending(r => r.AverageRating)
                .ThenBy(r => r.BusinessName)
                .ToList();
        }

        SetLocationViewBag(userLat, userLng, maxDist);
        return View(restaurants);
    }

    public async Task<IActionResult> Details(int id, double? lat, double? lng, double? maxDistance)
    {
        var (userLat, userLng, maxDist) = await ResolveLocationAsync(lat, lng, maxDistance);

        var caterer = await _context.CatererProfiles
            .Include(c => c.AppUser)
            .FirstOrDefaultAsync(c => c.Id == id);

        if (caterer == null || caterer.BusinessName.Contains("Archived", StringComparison.OrdinalIgnoreCase))
            return NotFound();

        var menuItems = await _context.MenuItems
            .Include(x => x.CustomizationGroups)
                .ThenInclude(g => g.Options)
            .Where(x => x.CatererProfileId == id && x.IsActive)
            .OrderBy(x => x.Name)
            .ToListAsync();

        if (!menuItems.Any()) return NotFound();

        var coverImage = GetRestaurantCoverImage(caterer.BusinessName);
        var distanceKm = CalculateDistanceForCaterer(userLat, userLng, caterer);

        SetLocationViewBag(userLat, userLng, maxDist);
        return View(new RestaurantDetailViewModel
        {
            Id = caterer.Id,
            BusinessName = caterer.BusinessName,
            Description = caterer.Description,
            Address = caterer.Address ?? caterer.AppUser.Address,
            CoverImageUrl = coverImage,
            AverageRating = caterer.AverageRating,
            DistanceKm = distanceKm,
            MenuItems = menuItems.Select(x => new NearbyMenuItemViewModel
            {
                Id = x.Id,
                Name = x.Name,
                Description = x.Description,
                Price = x.Price,
                ImageUrl = x.ImageUrl,
                CatererName = caterer.BusinessName,
                CatererProfileId = caterer.Id,
                DistanceKm = distanceKm,
                AverageRating = x.AverageRating,
                CatererLat = caterer.Latitude ?? 0,
                CatererLng = caterer.Longitude ?? 0,
                CustomizationGroups = x.CustomizationGroups
                    .OrderBy(g => g.DisplayOrder)
                    .Select(g => new HomeCustomizationGroupViewModel
                    {
                        Id = g.Id,
                        Name = g.Name,
                        IsRequired = g.IsRequired,
                        AllowsMultipleSelection = g.AllowsMultipleSelection,
                        Options = g.Options
                            .OrderBy(o => o.DisplayOrder)
                            .Select(o => new HomeCustomizationOptionViewModel
                            {
                                Id = o.Id,
                                Name = o.Name,
                                PriceModifier = o.PriceModifier,
                                IsRemovableIngredient = o.IsRemovableIngredient
                            })
                            .ToList()
                    })
                    .ToList()
            }).ToList()
        });
    }

    public IActionResult Privacy()
    {
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }

    /// <summary>
    /// Haversine formula to calculate distance between two lat/lng points in km.
    /// </summary>
    private static double CalculateDistance(double lat1, double lng1, double lat2, double lng2)
    {
        const double R = 6371; // Earth radius in km
        var dLat = ToRadians(lat2 - lat1);
        var dLng = ToRadians(lng2 - lng1);
        var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                Math.Cos(ToRadians(lat1)) * Math.Cos(ToRadians(lat2)) *
                Math.Sin(dLng / 2) * Math.Sin(dLng / 2);
        var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
        return R * c;
    }

    private static double ToRadians(double degrees) => degrees * Math.PI / 180;

    private async Task<(double? UserLat, double? UserLng, double MaxDistance)> ResolveLocationAsync(double? lat, double? lng, double? maxDistance)
    {
        if (lat.HasValue && lng.HasValue)
            return (lat, lng, maxDistance ?? 50);

        if (User.Identity?.IsAuthenticated == true && User.IsInRole("User"))
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var user = await _context.AppUsers.FindAsync(userId);
            return (user?.Latitude, user?.Longitude, maxDistance ?? 50);
        }

        return (lat, lng, maxDistance ?? 50);
    }

    private void SetLocationViewBag(double? userLat, double? userLng, double maxDistance)
    {
        ViewBag.UserLat = userLat;
        ViewBag.UserLng = userLng;
        ViewBag.MaxDistance = maxDistance;
        ViewBag.HasLocation = userLat.HasValue && userLng.HasValue;
    }

    private static double CalculateDistanceForCaterer(double? userLat, double? userLng, CatererProfile caterer)
    {
        if (userLat.HasValue && userLng.HasValue && caterer.Latitude.HasValue && caterer.Longitude.HasValue)
            return CalculateDistance(userLat.Value, userLng.Value, caterer.Latitude.Value, caterer.Longitude.Value);

        return 0;
    }

    private static string GetRestaurantCoverImage(string businessName)
    {
        return businessName switch
        {
            "Mutfak No. 34" => "/images/demo-restaurants/covers/mutfak-no-34.png",
            "Bosphorus Bites" => "/images/demo-restaurants/covers/bosphorus-bites.png",
            "Anatolia Kitchen" => "/images/demo-restaurants/covers/anatolia-kitchen.png",
            "Green Bowl Co." => "/images/demo-restaurants/covers/green-bowl-co.png",
            "Pide & Lahmacun House" => "/images/demo-restaurants/covers/pide-house.png",
            "Bella Pasta Catering" => "/images/demo-restaurants/covers/bella-pasta.png",
            "Sushi Box Istanbul" => "/images/demo-restaurants/covers/sushi-box.png",
            "Sweet Break Bakery" => "/images/demo-restaurants/covers/sweet-break.png",
            _ => "/images/no-image.svg"
        };
    }
}
