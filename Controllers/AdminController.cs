using CaterFlow.Data;
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
            return View();
        }
    }
}
