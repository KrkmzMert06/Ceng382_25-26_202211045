using System.ComponentModel.DataAnnotations;

namespace CaterFlow.Models.Entities
{
    public class SystemLog
    {
        public int Id { get; set; }

        [Required]
        [StringLength(50)]
        public string EventType { get; set; } = string.Empty;   // Auth, Order, Payment, Rating, Error, System

        [Required]
        [StringLength(100)]
        public string Action { get; set; } = string.Empty;      // e.g. "Login", "OrderCreated", "PaymentProcessed"

        [StringLength(500)]
        public string? Details { get; set; }

        public int? AppUserId { get; set; }

        [StringLength(150)]
        public string? UserEmail { get; set; }

        [StringLength(50)]
        public string? IpAddress { get; set; }

        [Required]
        [StringLength(20)]
        public string Level { get; set; } = "Info";             // Info, Warning, Error

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
