using Server.Models;
using System.Net.Sockets;

namespace Server.Services.Server
{
    public interface ITCPServerService
    {
        public Message Message { get; set; }
        public string ConnectionStatus { get; set; }
        public string ServerAddress { get; set; }
        public int ServerPort { get; set; }
        public string ClientAddress { get; set; }
        public int ClientPort { get; set; }
        public string ErrorMessage { get; set; }
        public bool IsClientConnected { get; set; }
        public event Action<Message> messageSent;
        public void OnMessageSent();
        public bool IsServerStopped { get; set; }
        Task StartServerAsync(CancellationToken cancellationToken);
        Task<bool> ValidateServerAddressAndPort(string serverAddress, int serverPort, CancellationToken cancellationToken);
        Task HandleClientAsync(TcpClient client, CancellationToken cancellationToken);
        Task SendMessageAsync(NetworkStream stream, Message message, CancellationToken cancellationToken);
        Task SendStringAsync(NetworkStream stream, string messageType, string message, CancellationToken cancellationToken);
        Task SendImageAsync(NetworkStream stream, byte[] image, CancellationToken cancellationToken);
        Task<string> ReceiveStringAsync(NetworkStream stream, CancellationToken cancellationToken);
    }
}
