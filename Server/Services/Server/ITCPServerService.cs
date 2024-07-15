using Server.Models;
using System.Collections.ObjectModel;
using System.Net.Sockets;

namespace Server.Services.Server
{
    public interface ITCPServerService
    {
        public Message Message { get; set; }
        public string ServerAddress { get; set; }
        public int ServerPort { get; set; }
        public string ErrorMessage { get; set; }
        public event Action<Message, TcpClient> messageSent;
        public event Action ClientConnected;
        public event Action ClientDisconnected;
        public void OnClientConnected();
        public void OnAllClientsDisconnected();
        public ObservableCollection<TcpClient> Clients { get; set; }
        public int ClientsCount { get; set; }
        public void OnMessageSent(TcpClient client);
        public bool IsServerStopped { get; set; }
        public CancellationTokenSource MessageFilled { get; set; }
        Task StartServerAsync(CancellationToken cancellationToken);
        Task<string> ReceiveDisconnectAsync(NetworkStream stream, CancellationToken linkedCancellationToken);
        Task<bool> ValidateServerAddressAndPort(string serverAddress, int serverPort, CancellationToken cancellationToken);
        Task HandleClientAsync(TcpClient client, CancellationToken cancellationToken);
        Task SendMessageAsync(NetworkStream stream, Message message, CancellationToken cancellationToken);
        Task SendStringAsync(NetworkStream stream, string messageType, string message, CancellationToken cancellationToken);
        Task SendImageAsync(NetworkStream stream, byte[] image, CancellationToken cancellationToken);
        Task<string> ReceiveStringAsync(NetworkStream stream, CancellationToken cancellationToken);
    }
}
