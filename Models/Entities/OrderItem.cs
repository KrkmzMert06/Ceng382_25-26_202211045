using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CaterFlow.Models.Entities
{
    public class OrderItem
    {
        public int Id { get; set; }

        [Required]
        public int OrderId { get; set; }

        [ForeignKey(nameof(OrderId))]
        public Order Order { get; set; } = null!;

        [Required]
        public int MenuItemId { get; set; }

        [ForeignKey(nameof(MenuItemId))]
        public MenuItem MenuItem { get; set; } = null!;

        [Required]
        [StringLength(120)]
        public string MenuItemName { get; set; } = string.Empty;

        [Column(TypeName = "decimal(18,2)")]
        public decimal UnitPrice { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal CustomizationTotal { get; set; }

        public int Quantity { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal LineTotal { get; set; }

        public ICollection<OrderItemCustomizationSelection> SelectedCustomizations { get; set; } = new List<OrderItemCustomizationSelection>();
    }
}
