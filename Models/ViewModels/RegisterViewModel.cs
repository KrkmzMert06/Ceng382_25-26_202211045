using System.ComponentModel.DataAnnotations;
using CaterFlow.Models.Entities;

namespace CaterFlow.Models.ViewModels
{
    public class RegisterViewModel
    {
        [Required]
        [StringLength(100)]
        [Display(Name = "Full Name")]
        public string FullName { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required]
        [MinLength(6)]
        [DataType(DataType.Password)]
        public string Password { get; set; } = string.Empty;

        [Required]
        [Compare(nameof(Password), ErrorMessage = "Passwords do not match.")]
        [DataType(DataType.Password)]
        [Display(Name = "Confirm Password")]
        public string ConfirmPassword { get; set; } = string.Empty;

        [Required]
        public UserRole Role { get; set; } = UserRole.User;

        [Display(Name = "Business Name")]
        [StringLength(120)]
        public string? BusinessName { get; set; }
    }
}
