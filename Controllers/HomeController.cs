using System.Diagnostics;
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

    [Authorize(Roles = "User,Admin")]
    public async Task<IActionResult> Index()
    {
        var items = await _context.MenuItems
            .Include(x => x.CatererProfile)
            .Include(x => x.CustomizationGroups)
                .ThenInclude(g => g.Options)
            .Where(x => x.IsActive)
            .OrderBy(x => x.Name)
            .Select(x => new HomeMenuItemViewModel
            {
                Name = x.Name,
                Description = x.Description,
                Price = x.Price,
                ImageUrl = x.ImageUrl,
                CatererName = x.CatererProfile.BusinessName,
                CustomizationGroups = x.CustomizationGroups
                    .OrderBy(g => g.DisplayOrder)
                    .Select(g => new HomeCustomizationGroupViewModel
                    {
                        Name = g.Name,
                        IsRequired = g.IsRequired,
                        AllowsMultipleSelection = g.AllowsMultipleSelection,
                        Options = g.Options
                            .OrderBy(o => o.DisplayOrder)
                            .Select(o => new HomeCustomizationOptionViewModel
                            {
                                Name = o.Name,
                                PriceModifier = o.PriceModifier,
                                IsRemovableIngredient = o.IsRemovableIngredient
                            })
                            .ToList()
                    })
                    .ToList()
            })
            .ToListAsync();

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
}
