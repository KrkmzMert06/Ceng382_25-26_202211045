using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace CaterFlow.Models.ViewModels
{
    public class MenuEditViewModel
    {
        public int Id { get; set; }

        [Required]
        [StringLength(120)]
        [Display(Name = "Menu Name")]
        public string Name { get; set; } = string.Empty;

        [StringLength(500)]
        public string? Description { get; set; }

        [Required]
        [Range(0.01, 999999)]
        public decimal Price { get; set; }

        public string ExistingImageUrl { get; set; } = string.Empty;

        [Display(Name = "Replace Menu Image")]
        public IFormFile? ImageFile { get; set; }

        [Display(Name = "Removable Ingredients")]
        public string? RemovableIngredientsText { get; set; }

        [Display(Name = "Customization Groups")]
        public string? CustomizationGroupsText { get; set; }

        public bool IsActive { get; set; } = true;
    }
}
