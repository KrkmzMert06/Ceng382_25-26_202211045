using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CaterFlow.Models.Entities
{
    public class MenuItem
    {
        public int Id { get; set; }

        [Required]
        public int CatererProfileId { get; set; }

        [ForeignKey("CatererProfileId")]
        public CatererProfile CatererProfile { get; set; } = null!;

        [Required]
        [StringLength(120)]
        public string Name { get; set; } = string.Empty;

        [StringLength(500)]
        public string? Description { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal Price { get; set; }

        [Required]
        [StringLength(250)]
        public string ImageUrl { get; set; } = string.Empty;

        public double AverageRating { get; set; } = 0;

        public bool IsActive { get; set; } = true;

        public ICollection<MenuItemCustomizationGroup> CustomizationGroups { get; set; } = new List<MenuItemCustomizationGroup>();
    }
}
