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
            var allRecords = _context.ShipperContactInfos.ToList();
            var activeRecords = _context.ShipperContactInfos
                .Where(x => x.Active == true)
                .ToList();

            ViewBag.ActiveRecords = activeRecords;

            return View(allRecords);
        }

        public IActionResult Edit(int id)
        {
            var record = _context.ShipperContactInfos.Find(id);
            if (record == null)
                return NotFound();

            return View(record);
        }

        [HttpPost]
        public IActionResult Edit(ShipperContactInfo model)
        {
            if (!ModelState.IsValid)
                return View(model);

            _context.ShipperContactInfos.Update(model);
            _context.SaveChanges();

            return RedirectToAction("Index");
        }

        public IActionResult Delete(int id)
        {
            var record = _context.ShipperContactInfos.Find(id);
            if (record == null)
                return NotFound();

            _context.ShipperContactInfos.Remove(record);
            _context.SaveChanges();

            return RedirectToAction("Index");
        }
    }
}