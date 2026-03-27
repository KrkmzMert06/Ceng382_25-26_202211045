namespace MyRazorAuthApp.Models.Media
{
    public class ImageModel
    {
        public int Id { get; set; }
        public string? FileName { get; set; }
        public string? ContentType { get; set; }
        public long Size { get; set; }
        public byte[]? Data { get; set; }
    }
}