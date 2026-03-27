using Microsoft.AspNetCore.Identity;

namespace MyRazorAuthApp.Models.Identity
{
    public class ApplicationUser : IdentityUser
    {
        public byte[]? ProfilePicture { get; set; }
    }
}