using System.Security.Claims;
using CaterFlow.Data;
using CaterFlow.Models.Entities;
using CaterFlow.Models.ViewModels;
using CaterFlow.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CaterFlow.Controllers
{
    public class AccountController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly PasswordService _passwordService;
        private readonly LoggingService _loggingService;

        public AccountController(ApplicationDbContext context, PasswordService passwordService, LoggingService loggingService)
        {
            _context = context;
            _passwordService = passwordService;
            _loggingService = loggingService;
        }

        [HttpGet]
        public IActionResult Register()
        {
            if (User.Identity?.IsAuthenticated == true)
                return RedirectByRole();

            return View(new RegisterViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            // Admin accounts cannot be created through registration — only via seed data
            if (model.Role == UserRole.Admin)
                model.Role = UserRole.User;

            if (model.Role == UserRole.Caterer && string.IsNullOrWhiteSpace(model.BusinessName))
                ModelState.AddModelError(nameof(model.BusinessName), "Business name is required for caterers.");

            if (await _context.AppUsers.AnyAsync(x => x.Email == model.Email))
                ModelState.AddModelError(nameof(model.Email), "This email is already registered.");

            if (!ModelState.IsValid)
                return View(model);

            var user = new AppUser
            {
                FullName = model.FullName.Trim(),
                Email = model.Email.Trim().ToLowerInvariant(),
                PasswordHash = _passwordService.HashPassword(model.Password),
                Role = model.Role
            };

            _context.AppUsers.Add(user);
            await _context.SaveChangesAsync();

            if (user.Role == UserRole.Caterer)
            {
                _context.CatererProfiles.Add(new CatererProfile
                {
                    AppUserId = user.Id,
                    BusinessName = model.BusinessName!.Trim(),
                    Description = $"{model.BusinessName} menu creator"
                });
                await _context.SaveChangesAsync();
            }

            await _loggingService.LogAuthAsync("Register", user.Id, user.Email,
                HttpContext.Connection.RemoteIpAddress?.ToString(),
                $"New {user.Role} registered: {user.FullName}");

            await SignInAsync(user);
            return RedirectByRole(user.Role);
        }

        [HttpGet]
        public IActionResult Login()
        {
            if (User.Identity?.IsAuthenticated == true)
                return RedirectByRole();

            return View(new LoginViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var user = await _context.AppUsers.FirstOrDefaultAsync(x => x.Email == model.Email.Trim().ToLowerInvariant());
            if (user == null || !_passwordService.VerifyPassword(model.Password, user.PasswordHash))
            {
                ModelState.AddModelError(string.Empty, "Invalid email or password.");

                await _loggingService.LogAuthAsync("LoginFailed", null, model.Email.Trim(),
                    HttpContext.Connection.RemoteIpAddress?.ToString(),
                    "Invalid credentials attempt");

                return View(model);
            }

            await _loggingService.LogAuthAsync("Login", user.Id, user.Email,
                HttpContext.Connection.RemoteIpAddress?.ToString(),
                $"User logged in: {user.FullName}");

            await SignInAsync(user);
            return RedirectByRole(user.Role);
        }

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var email = User.FindFirstValue(ClaimTypes.Email);

            await _loggingService.LogAuthAsync("Logout",
                int.TryParse(userId, out var uid) ? uid : null,
                email,
                HttpContext.Connection.RemoteIpAddress?.ToString());

            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Login", "Account");
        }

        [Authorize]
        [HttpGet]
        public async Task<IActionResult> UpdateLocation()
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var user = await _context.AppUsers.FindAsync(userId);
            if (user == null) return NotFound();

            ViewBag.CurrentLat = user.Latitude;
            ViewBag.CurrentLng = user.Longitude;
            ViewBag.CurrentAddress = user.Address;
            return View();
        }

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateLocation(double latitude, double longitude, string? address)
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var user = await _context.AppUsers.FindAsync(userId);
            if (user == null) return NotFound();

            user.Latitude = latitude;
            user.Longitude = longitude;
            user.Address = address?.Trim();
            await _context.SaveChangesAsync();

            // If caterer, also update caterer profile location
            if (user.Role == UserRole.Caterer)
            {
                var catererProfile = await _context.CatererProfiles.FirstOrDefaultAsync(c => c.AppUserId == userId);
                if (catererProfile != null)
                {
                    catererProfile.Latitude = latitude;
                    catererProfile.Longitude = longitude;
                    catererProfile.Address = address?.Trim();
                    await _context.SaveChangesAsync();
                }
            }

            // Re-sign-in to refresh the Address claim in the cookie
            await SignInAsync(user);

            TempData["Success"] = "Location updated successfully.";
            return RedirectByRole();
        }

        private async Task SignInAsync(AppUser user)
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Name, user.FullName),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim(ClaimTypes.Role, user.Role.ToString()),
                new Claim(ClaimTypes.StreetAddress, user.Address ?? "")
            };

            var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var principal = new ClaimsPrincipal(identity);
            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal);
        }

        private IActionResult RedirectByRole()
        {
            var role = User.FindFirstValue(ClaimTypes.Role);
            return RedirectByRole(Enum.TryParse<UserRole>(role, out var parsedRole) ? parsedRole : UserRole.User);
        }

        private IActionResult RedirectByRole(UserRole role)
        {
            return role switch
            {
                UserRole.Admin => RedirectToAction("Index", "Admin"),
                UserRole.Caterer => RedirectToAction("MyItems", "Menu"),
                _ => RedirectToAction("Index", "Home")
            };
        }
    }
}
