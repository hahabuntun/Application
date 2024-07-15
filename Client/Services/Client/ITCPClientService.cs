

using Client.Models;
using System.Collections.ObjectModel;
using System.Net.Sockets;

namespace Client.Services.Client
{
    public interface ITCPClientService
    {
        public string ServerAddress { get; set; }
        public int ServerPort { get; set; }
        public string ClientAddress { get; set; }
        public int ClientPort { get; set; }
        public bool IsConnected { get; set; }
        public Message Message { get; set; }
        public event Action<Message> messageReceived;
        public event Action serverDisconnected;
        public void OnServerDisconnected();

        public string ConnectionStatus { get; set; }
        public string ErrorMessage { get; set; }
        
        public CancellationTokenSource ResendCancellationTokenSource { get; set; }
        public Task StartClientAsync(CancellationToken cancellationToken);
        public Task SendStringAsync(NetworkStream stream, string message, CancellationToken cancellationToken);
        public Task<Message> ReceiveMessage(NetworkStream stream, CancellationToken cancellationToken);
        public Task<byte[]> ReceiveImageAsync(NetworkStream stream, CancellationToken cancellationToken);
        public Task<string> ReceiveStringAsync(NetworkStream stream, CancellationToken cancellationToken);
        public Task<string> ReceiveDisconnectAsync(NetworkStream stream, CancellationToken linkedCancellationToken);
    }
}
