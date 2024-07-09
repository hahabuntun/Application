namespace Client.Models
{
    /// <summary>
    /// Данные, которые будут отображаться на форме
    /// </summary>
    public class Message
    {
        public string From { get; set; }
        public DateTime Time { get; set; }
        public string Text { get; set; }
        public string ImagePath { get; set; }
        public string Color { get; set; }
    }
}
