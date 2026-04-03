using Microsoft.AspNetCore.Mvc;
using NorthwindWeb.Models;
using System.Linq;

namespace NorthwindWeb.Controllers
{
    public class HomeController : Controller
    {
        private readonly NorthwindContext _context;

        public HomeController(NorthwindContext context)
        {
            _context = context;
        }

        public IActionResult Index()
        {
            var data = _context.ShipperContactInfos.ToList();
            return View(data);
        }
    }
}