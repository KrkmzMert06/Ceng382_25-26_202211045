using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using MyRazorAuthApp.Data;
using MyRazorAuthApp.Models.Identity;
using MyRazorAuthApp.Models.Media;
using System.Linq;

namespace MyRazorAuthApp.Pages
{
    public class IndexModel : PageModel
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ApplicationDbContext _context;

        public List<ApplicationUser> Users { get; set; } = new();
        public List<ImageModel> Images { get; set; } = new();

        [BindProperty]
        public IFormFile? UploadedImage { get; set; }

        public IndexModel(UserManager<ApplicationUser> userManager, ApplicationDbContext context)
        {
            _userManager = userManager;
            _context = context;
        }

        public void OnGet()
        {
            Users = _userManager.Users.ToList();
            Images = _context.Images.ToList();
        }

        public async Task<IActionResult> OnPostUploadAsync()
        {
            if (UploadedImage != null)
            {
                using var ms = new MemoryStream();
                await UploadedImage.CopyToAsync(ms);

                var image = new ImageModel
                {
                    FileName = UploadedImage.FileName,
                    ContentType = UploadedImage.ContentType,
                    Size = UploadedImage.Length,
                    Data = ms.ToArray()
                };

                _context.Images.Add(image);
                await _context.SaveChangesAsync();
            }

            return RedirectToPage();
        }
    }
}