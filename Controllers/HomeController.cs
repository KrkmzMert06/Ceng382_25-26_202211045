using System.Diagnostics;
using System.Security.Claims;
using CaterFlow.Data;
using CaterFlow.Models;
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

    [Authorize(Roles = "User")]
    public async Task<IActionResult> Index(double? lat, double? lng, double? maxDistance)
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var user = await _context.AppUsers.FindAsync(userId);

        // Use provided coordinates or user's saved location
        double? userLat = lat ?? user?.Latitude;
        double? userLng = lng ?? user?.Longitude;
        double maxDist = maxDistance ?? 50; // default 50 km

        ViewBag.UserLat = userLat;
        ViewBag.UserLng = userLng;
        ViewBag.MaxDistance = maxDist;
        ViewBag.HasLocation = userLat.HasValue && userLng.HasValue;

        var query = _context.MenuItems
            .Include(x => x.CatererProfile)
                .ThenInclude(c => c.AppUser)
            .Include(x => x.CustomizationGroups)
                .ThenInclude(g => g.Options)
            .Where(x => x.IsActive);

        var menuItems = await query.OrderBy(x => x.Name).ToListAsync();

        var items = menuItems.Select(x =>
        {
            double distanceKm = 0;
            if (userLat.HasValue && userLng.HasValue &&
                x.CatererProfile.Latitude.HasValue && x.CatererProfile.Longitude.HasValue)
            {
                distanceKm = CalculateDistance(
                    userLat.Value, userLng.Value,
                    x.CatererProfile.Latitude.Value, x.CatererProfile.Longitude.Value);
            }

            return new NearbyMenuItemViewModel
            {
                Id = x.Id,
                Name = x.Name,
                Description = x.Description,
                Price = x.Price,
                ImageUrl = x.ImageUrl,
                CatererName = x.CatererProfile.BusinessName,
                CatererProfileId = x.CatererProfileId,
                DistanceKm = distanceKm,
                AverageRating = x.AverageRating,
                CatererLat = x.CatererProfile.Latitude ?? 0,
                CatererLng = x.CatererProfile.Longitude ?? 0,
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
            };
        }).ToList();

        // Filter by distance if user has location
        if (userLat.HasValue && userLng.HasValue)
        {
            items = items
                .Where(x => x.CatererLat != 0 && x.CatererLng != 0 && x.DistanceKm <= maxDist)
                .OrderBy(x => x.DistanceKm)
                .ToList();
        }

        return View(items);
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
}
