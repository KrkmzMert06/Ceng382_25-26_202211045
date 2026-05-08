namespace CaterFlow.Models.ViewModels
{
    public class LogListViewModel
    {
        public List<LogItemViewModel> Logs { get; set; } = new();
        public string? FilterEventType { get; set; }
        public string? FilterLevel { get; set; }
        public string? SearchTerm { get; set; }
        public int CurrentPage { get; set; } = 1;
        public int TotalPages { get; set; }
        public int PageSize { get; set; } = 25;
    }

    public class LogItemViewModel
    {
        public int Id { get; set; }
        public string EventType { get; set; } = string.Empty;
        public string Action { get; set; } = string.Empty;
        public string? Details { get; set; }
        public string? UserEmail { get; set; }
        public string Level { get; set; } = "Info";
        public string? IpAddress { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
