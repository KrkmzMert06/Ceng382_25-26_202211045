using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CaterFlow.Models.Entities
{
    public class Rating
    {
        public int Id { get; set; }

        [Required]
        public int OrderId { get; set; }

        [ForeignKey(nameof(OrderId))]
        public Order Order { get; set; } = null!;

        [Required]
        public int AppUserId { get; set; }

        [ForeignKey(nameof(AppUserId))]
        public AppUser AppUser { get; set; } = null!;

        // Nullable – set when rating a specific menu item
        public int? MenuItemId { get; set; }

        [ForeignKey(nameof(MenuItemId))]
        public MenuItem? MenuItem { get; set; }

        // Nullable – set when rating a caterer
        public int? CatererProfileId { get; set; }

        [ForeignKey(nameof(CatererProfileId))]
        public CatererProfile? CatererProfile { get; set; }

        [Range(1, 5)]
        public int Score { get; set; }

        [StringLength(500)]
        public string? Comment { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
