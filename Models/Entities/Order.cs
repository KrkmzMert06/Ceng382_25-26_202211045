using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CaterFlow.Models.Entities
{
    public class Order
    {
        public int Id { get; set; }

        [Required]
        public int AppUserId { get; set; }

        [ForeignKey(nameof(AppUserId))]
        public AppUser AppUser { get; set; } = null!;

        [Column(TypeName = "decimal(18,2)")]
        public decimal TotalPrice { get; set; }

        public OrderStatus Status { get; set; } = OrderStatus.Pending;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? PaidAt { get; set; }

        [StringLength(120)]
        public string? CardHolderName { get; set; }

        [StringLength(4)]
        public string? CardLastFourDigits { get; set; }

        public ICollection<OrderItem> Items { get; set; } = new List<OrderItem>();
    }
}
