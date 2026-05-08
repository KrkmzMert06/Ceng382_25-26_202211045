using System.ComponentModel.DataAnnotations;

namespace CaterFlow.Models.ViewModels
{
    public class RateOrderViewModel
    {
        public int OrderId { get; set; }
        public List<RateMenuItemRow> MenuItems { get; set; } = new();
        public RateCatererRow? Caterer { get; set; }
    }

    public class RateMenuItemRow
    {
        public int MenuItemId { get; set; }
        public string MenuItemName { get; set; } = string.Empty;
        public string ImageUrl { get; set; } = string.Empty;

        [Range(1, 5, ErrorMessage = "Please select a rating between 1 and 5.")]
        public int Score { get; set; }

        [StringLength(500)]
        public string? Comment { get; set; }

        public bool AlreadyRated { get; set; }
        public int? ExistingScore { get; set; }
        public string? ExistingComment { get; set; }
    }

    public class RateCatererRow
    {
        public int CatererProfileId { get; set; }
        public string CatererName { get; set; } = string.Empty;

        [Range(1, 5, ErrorMessage = "Please select a rating between 1 and 5.")]
        public int Score { get; set; }

        [StringLength(500)]
        public string? Comment { get; set; }

        public bool AlreadyRated { get; set; }
        public int? ExistingScore { get; set; }
        public string? ExistingComment { get; set; }
    }

    public class RatingDisplayViewModel
    {
        public int Score { get; set; }
        public string? Comment { get; set; }
        public string UserName { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
    }
}
