namespace CaterFlow.Models.ViewModels
{
    public class NearbyRestaurantViewModel
    {
        public int Id { get; set; }
        public string BusinessName { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string? Address { get; set; }
        public string CoverImageUrl { get; set; } = string.Empty;
        public double DistanceKm { get; set; }
        public double AverageRating { get; set; }
        public int MenuItemCount { get; set; }
        public decimal StartingPrice { get; set; }
        public double CatererLat { get; set; }
        public double CatererLng { get; set; }
    }

    public class RestaurantDetailViewModel
    {
        public int Id { get; set; }
        public string BusinessName { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string? Address { get; set; }
        public string CoverImageUrl { get; set; } = string.Empty;
        public double DistanceKm { get; set; }
        public double AverageRating { get; set; }
        public List<NearbyMenuItemViewModel> MenuItems { get; set; } = new();
    }

    public class NearbyMenuItemViewModel
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public decimal Price { get; set; }
        public string ImageUrl { get; set; } = string.Empty;
        public string CatererName { get; set; } = string.Empty;
        public int CatererProfileId { get; set; }
        public double DistanceKm { get; set; }
        public double AverageRating { get; set; }
        public double CatererLat { get; set; }
        public double CatererLng { get; set; }
        public List<HomeCustomizationGroupViewModel> CustomizationGroups { get; set; } = new();
    }
}
