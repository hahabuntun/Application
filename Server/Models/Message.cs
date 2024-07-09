namespace Server.Models
{
    /// <summary>
    /// Данные, которые будут отображены на окне связи с клиентом
    /// </summary>
    public class Message
    {
        public int? Id { get; set; }
        public string? Text { get; set; }
        public string? From { get; set; }
        public string? To { get; set; }
        public string? FormatVersion { get; set; }
        public string? Color { get; set; }
        public byte[]? ImageBytes { get; set; }
        public string ImagePath { get; set; }
    }
}
