using System.Net.Sockets;

namespace Server.Models
{
    /// <summary>
    /// Данные, отображаемые на окне всех сообщений сессии с клиентом
    /// </summary>
    public class StoredMessage
    {
        public TcpClient Client { get; set; }
        public string ServerAddress { get; set; }
        public int ServerPort { get; set; }
        public string ClientAddress { get; set; }
        public int ClientPort { get; set; }
        public int? Id { get; set; }
        public string Text { get; set; }
        public string From { get; set; }
        public string To { get; set; }
        public string FormatVersion { get; set; }
        public DateTime Time { get; set; }
        public string Color { get; set; }
        public string ImagePath { get; set; }
    }
}
