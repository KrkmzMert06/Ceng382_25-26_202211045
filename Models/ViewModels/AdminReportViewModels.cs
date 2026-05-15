namespace CaterFlow.Models.ViewModels
{
    public class AdminReportViewModel
    {
        public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;
        public DateTime? From { get; set; }
        public DateTime? To { get; set; }
        public string? StatusFilter { get; set; }
        public string? SearchTerm { get; set; }
        public int CurrentPage { get; set; } = 1;
        public int TotalPages { get; set; }
        public int PageSize { get; set; } = 20;
        public int TotalOrders { get; set; }
        public decimal TotalRevenue { get; set; }
        public decimal AverageOrderValue { get; set; }
        public int TotalItemsSold { get; set; }
        public int CompletedOrders { get; set; }
        public int RatingCount { get; set; }
        public double AverageMenuRating { get; set; }
        public List<AdminReportOrderViewModel> Orders { get; set; } = new();
        public List<AdminReportMenuItemViewModel> TopMenuItems { get; set; } = new();
    }

    public class AdminReportOrderViewModel
    {
        public int Id { get; set; }
        public DateTime CreatedAt { get; set; }
        public string CustomerEmail { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public int ItemCount { get; set; }
        public decimal TotalPrice { get; set; }
    }

    public class AdminReportMenuItemViewModel
    {
        public string Name { get; set; } = string.Empty;
        public int QuantitySold { get; set; }
        public decimal Revenue { get; set; }
    }
}
