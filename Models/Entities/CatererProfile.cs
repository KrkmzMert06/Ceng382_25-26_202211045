using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CaterFlow.Models.Entities
{
    public class CatererProfile
    {
        public int Id { get; set; }

        [Required]
        public int AppUserId { get; set; }

        [ForeignKey("AppUserId")]
        public AppUser AppUser { get; set; } = null!;

        [Required]
        [StringLength(120)]
        public string BusinessName { get; set; } = string.Empty;

        [StringLength(500)]
        public string? Description { get; set; }

        [StringLength(250)]
        public string? Address { get; set; }

        public double? Latitude { get; set; }
        public double? Longitude { get; set; }

        public double AverageRating { get; set; } = 0;
    }
}