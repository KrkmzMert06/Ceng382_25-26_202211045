using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CaterFlow.Models.Entities
{
    public class OrderItemCustomizationSelection
    {
        public int Id { get; set; }

        [Required]
        public int OrderItemId { get; set; }

        [ForeignKey(nameof(OrderItemId))]
        public OrderItem OrderItem { get; set; } = null!;

        public int? MenuItemCustomizationOptionId { get; set; }

        [ForeignKey(nameof(MenuItemCustomizationOptionId))]
        public MenuItemCustomizationOption? MenuItemCustomizationOption { get; set; }

        [Required]
        [StringLength(120)]
        public string GroupName { get; set; } = string.Empty;

        [Required]
        [StringLength(120)]
        public string OptionName { get; set; } = string.Empty;

        [Column(TypeName = "decimal(18,2)")]
        public decimal PriceModifier { get; set; }

        public bool IsRemovableIngredient { get; set; }
    }
}
