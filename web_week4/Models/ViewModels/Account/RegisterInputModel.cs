using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;

namespace MyRazorAuthApp.Models.ViewModels.Account
{
    public class RegisterInputModel
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        public IFormFile? ProfilePicture { get; set; }

        [Required]
        [StringLength(100, MinimumLength = 6)]
        [DataType(DataType.Password)]
        public string Password { get; set; } = string.Empty;

        [DataType(DataType.Password)]
        [Compare("Password")]
        public string ConfirmPassword { get; set; } = string.Empty;
    }
}