using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CaterFlow.Models.Entities
{
    public class MenuItemCustomizationOption
    {
        public int Id { get; set; }

        [Required]
        public int MenuItemCustomizationGroupId { get; set; }

        [ForeignKey(nameof(MenuItemCustomizationGroupId))]
        public MenuItemCustomizationGroup Group { get; set; } = null!;

        [Required]
        [StringLength(120)]
        public string Name { get; set; } = string.Empty;

        [Column(TypeName = "decimal(18,2)")]
        public decimal PriceModifier { get; set; }

        public bool IsRemovableIngredient { get; set; }

        public bool IsDefaultSelected { get; set; }

        public int DisplayOrder { get; set; }
    }
}
