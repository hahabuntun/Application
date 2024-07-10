using Microsoft.Extensions.Logging;
using Server.Exceptions;
using Server.Helpers;
using Server.Models;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Text;

namespace Server.Services.Server
{
    /// <summary>
    /// Обеспечивает связь с клиентом
    /// Связь должна быть в строгом порядке
    /// Ожидается подключение клиента => ожидается загрузка сообщения => сообщение отправляется клиентку => по приходе запроса resend осуществляется повторная отправка
    /// По приходе запроса disconnect сервер прослушивает новго клиента
    /// </summary>
    /// 
    public class TCPServerService : ITCPServerService, INotifyPropertyChanged
    {
        private ObservableCollection<string> _availableAddresses = new ObservableCollection<string>(); //на каких адрессах мы можем запуститься
        private readonly ILogger<TCPServerService> _logger;
        private string _errorMessage = "";
        private bool _isClientConnected = false;
        private bool _isServerStopped = true;
        private string _clientAddress;
        private int _clientPort;
        private string _serverAddress;
        private int _serverPort;
        private string _connectionStatus = "Not connected";
        private Message _message = null;


        public string ConnectionStatus { get => _connectionStatus; set {  _connectionStatus = value; OnPropertyChanged(); } }
        public string ServerAddress { get => _serverAddress; set { _serverAddress = value; OnPropertyChanged(); } }
        public int ServerPort { get => _serverPort; set { _serverPort = value; OnPropertyChanged(); } }
        public string ClientAddress { get => _clientAddress; set { _clientAddress = value; OnPropertyChanged(); } }
        public int ClientPort { get => _clientPort; set { _clientPort = value; OnPropertyChanged(); } }
        public string ErrorMessage { get => _errorMessage; set { _errorMessage = value; OnPropertyChanged(); } }
        public bool IsClientConnected { get => _isClientConnected; set { _isClientConnected = value; OnPropertyChanged(); } }
        public bool IsServerStopped { get => _isServerStopped; set { _isServerStopped = value; OnPropertyChanged(); } }
        public ObservableCollection<string> AvailableAddresses { get => _availableAddresses; set { _availableAddresses = value; OnPropertyChanged(); } }
        public Message Message { get => _message; set { _message = value; OnPropertyChanged(); } } // отсылаемое сообщение

        public event Action<Message> messageSent; 



        public TCPServerService(ILogger<TCPServerService> logger)
        {
            _logger = logger;
            random();
        }



        /// <summary>
        /// Вызывается при отсылке сообщения, чтобы обновить окно всех сообщений
        /// </summary>
        public void OnMessageSent()
        {
            messageSent?.Invoke(Message);
        }
        /// <summary>
        /// Формирует список доступных ip для сервера
        /// </summary>
        public void random()
        {
            List<IPAddress> addresses = new List<IPAddress>();

            // 1. Get all network interfaces
            NetworkInterface[] interfaces = NetworkInterface.GetAllNetworkInterfaces();

            // 2. Iterate through each interface
            foreach (NetworkInterface networkInterface in interfaces)
            {
                // 3. Skip loopback and virtual interfaces
                if (networkInterface.NetworkInterfaceType == NetworkInterfaceType.Loopback ||
                    networkInterface.NetworkInterfaceType == NetworkInterfaceType.Tunnel)
                {
                    continue;
                }

                // 4. Get all IP addresses associated with the interface
                IPInterfaceProperties interfaceProperties = networkInterface.GetIPProperties();
                UnicastIPAddressInformationCollection ipAddresses = interfaceProperties.UnicastAddresses;

                // 5. Filter addresses:
                //   - Check if the address is not in the loopback address family
                //   - Check if the address is not in the link-local address range (169.254.0.0 - 169.254.255.255)
                addresses.AddRange(ipAddresses.Where(ipAddressInfo =>
                    ipAddressInfo.Address.AddressFamily != AddressFamily.InterNetworkV6 &&
                    !ipAddressInfo.Address.IsInRange(IPAddress.Parse("169.254.0.0"), IPAddress.Parse("169.254.255.255"))
                ).Select(ipAddressInfo => ipAddressInfo.Address));
            }

            foreach (IPAddress address in addresses)
            {
                AvailableAddresses.Add(address.ToString());
            }
        }
        

        /// <summary>
        /// Точка входа. Ждет подключения клиентов и начинает их обработку
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public async Task StartServerAsync(CancellationToken cancellationToken)
        {
            ErrorMessage = "";
            _logger.LogInformation("Starting server at {ServerAddress}:{ServerPort}", ServerAddress, ServerPort);

            //проверяем введенные адресс и порт
            if (!await ValidateServerAddressAndPort(ServerAddress, ServerPort, cancellationToken))
            {
                ErrorMessage = "Invalid server address or port";
                _logger.LogError(ErrorMessage);
                return;
            }


            IPAddress localAddress = IPAddress.Parse(ServerAddress);
            try
            {
                using (TcpListener tcpListener = new TcpListener(localAddress, ServerPort)) //создаем слушателя
                {
                    tcpListener.Start(); //начинаем работу
                    IsServerStopped = false;
                    TcpClient client;
                    _logger.LogInformation("Server started successfully");

                    while (!cancellationToken.IsCancellationRequested) //пока работа не приостановлена слушаем клиентов
                    {
                        using (client = await tcpListener.AcceptTcpClientAsync(cancellationToken))
                        {
                            ConnectionStatus = "Connected";
                            IsClientConnected = true;
                            _logger.LogInformation("Client connected from {ClientAddress}:{ClientPort}", ClientAddress, ClientPort);

                            try
                            {
                                await HandleClientAsync(client, cancellationToken); //обрабатываем клиента
                            }
                            //перехватываем исключение об отключении клиента
                            catch (ClientDisconnectedException ex)
                            {
                                _logger.LogWarning(ex, "Client disconnected");
                            }
                            //обнуляем информацию для отображения
                            finally
                            {
                                IsClientConnected = false;
                                ClientAddress = "";
                                ConnectionStatus = "Not connected";
                                ClientPort = 0;
                                Message = null;
                                _logger.LogInformation("Client connection closed");
                            }
                        }
                    }
                }
            }
            //перехватываем необработанные исключения
            catch (Exception ex)
            {
                ErrorMessage = ex.Message;
                _logger.LogError(ex, "An error occurred while running the server");
            }
            //обнуляем информацию для отображения
            finally
            {
                IsClientConnected = false;
                IsServerStopped = true;
                ConnectionStatus = "Not connected";
                ClientAddress = "";
                ClientPort = 0;
                Message = null;
                _logger.LogInformation("Server stopped");
            }
        }
        /// <summary>
        /// Ведет диалог с клиентом
        /// </summary>
        /// <param name="client"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        /// <exception cref="ClientDisconnectedException"></exception>
        public async Task HandleClientAsync(TcpClient client, CancellationToken cancellationToken)
        {
            //получаем данные клиента(адресс и порт)
            IPEndPoint? clientEndPoint = client.Client.RemoteEndPoint as IPEndPoint;
            ClientAddress = clientEndPoint.Address.ToString();
            ClientPort = clientEndPoint.Port;

            //создаем поток для связи с клиентом
            using (NetworkStream stream = client.GetStream())
            {
                try
                {
                    //Ждем пока появится сообщение(обработается xml файл)
                    while (Message == null)
                    {
                        //выходим из цикла если сервер остановлен
                        if (cancellationToken.IsCancellationRequested)
                        {
                            throw new OperationCanceledException();
                        }
                    }
                    await SendMessageAsync(stream, Message, cancellationToken); //Отправляем обработанные данные клиенту
                    OnMessageSent(); // сообщаем подписчикам об изменении
                    _logger.LogInformation("message was sent to the client");

                    string clientRequest;
                    //ждем запроса на пересылку данных или на выключение
                    while (!cancellationToken.IsCancellationRequested) //пока сервер не остановлен
                    {
                        clientRequest = await ReceiveStringAsync(stream, cancellationToken); //слушаем запросы от клиента
                        if (clientRequest == "resend") //если клиент запросил resend
                        {
                            await SendMessageAsync(stream, Message, cancellationToken); //отправляем ему сообщение повторно
                            OnMessageSent(); // оповещаем подписчиков об отправке сообщения
                        }
                        else if (clientRequest == "disconnect") //если клиент отправил запрос disconnect
                        {
                            //stream?.Close();
                            throw new ClientDisconnectedException(); //выбрасываем исключение об отключении клиента
                        }
                    }
                    await SendStringAsync(stream, "disconnect", "", CancellationToken.None); //отправляем строку disconnect на сервер(нельзя отменить операцию)
                    return;
                }
                catch (OperationCanceledException)
                {
                    await SendStringAsync(stream, "disconnect", "", CancellationToken.None); //отправляем строку disconnect на сервер(нельзя отменить операцию)
                    return;
                }
            }
        }
        /// <summary>
        /// Проверка валидности адресса и порта сервера. Пока не реализована
        /// </summary>
        /// <param name="serverAddress"></param>
        /// <param name="serverPort"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public async Task<bool> ValidateServerAddressAndPort(string serverAddress, int serverPort, CancellationToken cancellationToken)
        {
            if (serverAddress == "" || serverPort == 0)
            {
                return false;
            }
            else
            {
                return true;
            }
        }
        /// <summary>
        /// Поулчение сообщения от клиента
        /// </summary>
        /// <param name="stream"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        /// <exception cref="ClientDisconnectedException"></exception>
        public async Task<string> ReceiveStringAsync(NetworkStream stream, CancellationToken cancellationToken)
        {
            byte[] requestBytes = new byte[1024];
            string request;
            int bytesRead;
            bytesRead = await stream.ReadAsync(requestBytes, 0, requestBytes.Length, cancellationToken); //получаем данные от клиента
            request = Encoding.UTF8.GetString(requestBytes, 0, bytesRead);
            return request;
        }
        /// <summary>
        /// Функция отправки данных клиенту.
        /// Поочередно отправляет необходимые данные клиенту
        /// Проверяя готов ли клиент их получить
        /// </summary>
        /// <param name="stream"></param>
        /// <param name="message"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        /// <exception cref="ClientDisconnectedException"></exception>
        public async Task SendMessageAsync(NetworkStream stream, Message message, CancellationToken cancellationToken)
        {
            //отправляем текст
            if (message.Text != null && message.Text != "")
                await SendStringAsync(stream, "is-ready-to-rec", message.Text, cancellationToken);
            else
                await SendStringAsync(stream, "is-ready-to-rec", "no-data", cancellationToken);
            //отправляем цвет
            if (message.Color != null && message.Color != "")
                await SendStringAsync(stream, "is-ready-to-rec", message.Color, cancellationToken);
            else
                await SendStringAsync(stream, "is-ready-to-rec", "no-data", cancellationToken);
            //отправляем From
            if (message.From != null && message.From != "")
                await SendStringAsync(stream, "is-ready-to-rec", message.From, cancellationToken);
            else
                await SendStringAsync(stream, "is-ready-to-rec", "no-data", cancellationToken);
            //отправляем картинку
            if (message.ImageBytes.Length > 0)
                await SendImageAsync(stream, message.ImageBytes, cancellationToken);
            else
                await SendStringAsync(stream, "is-ready-to-rec", "no-data", cancellationToken);
        }
        /// <summary>
        /// Отправка картинки клиенту
        /// Запрос клиенту на готовность получения => отправка длины картинки => отправка самой картинки
        /// </summary>
        /// <param name="stream"></param>
        /// <param name="image"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        /// <exception cref="ClientDisconnectedException"></exception>
        public async Task SendImageAsync(NetworkStream stream, byte[] image, CancellationToken cancellationToken)
        {
            //отправляем сообщение, с проверкой готовности получения данных клиентом
            byte[] isReadyBuffer = Encoding.UTF8.GetBytes("messageType: is-ready-to-rec;");
            await stream.WriteAsync(isReadyBuffer, 0, isReadyBuffer.Length, cancellationToken);

            string clientResponse = await ReceiveStringAsync(stream, cancellationToken); //получаем ответ от клиента
            
            //если ответ ready, то отправляем длину данных, а затем сами данные
            if (clientResponse == "ready")
            {
                byte[] imageLengthBytes = BitConverter.GetBytes(image.Length);
                await stream.WriteAsync(imageLengthBytes, 0, imageLengthBytes.Length, cancellationToken);
                await stream.FlushAsync(cancellationToken);
                await stream.WriteAsync(image, 0, image.Length, cancellationToken);
                await stream.FlushAsync(cancellationToken);
            }
            //если ответ disconnect, то выбрасываем исключение об отключении клиента
            else if (clientResponse == "disconnect")
            {
                throw new ClientDisconnectedException();
            }
        }
        /// <summary>
        /// Отправка строки клиенту
        /// Запрос клиенту на готовность получения => отправка длины строки => отправка строки
        /// </summary>
        /// <param name="stream"></param>
        /// <param name="messageType"></param>
        /// <param name="message"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        /// <exception cref="ClientDisconnectedException"></exception>
        public async Task SendStringAsync(NetworkStream stream, string messageType, string message, CancellationToken cancellationToken)
        {
            //если мы отправляем данные клиенту
            if (messageType == "is-ready-to-rec")
            {
                //отправляем клиенту строку для проверки его готовности к получению данных
                byte[] isReadyBuffer = Encoding.UTF8.GetBytes("messageType: is-ready-to-rec;");
                byte[] stringBytes = Encoding.UTF8.GetBytes(message);
                byte[] lengthBytes = BitConverter.GetBytes(stringBytes.Length);
                await stream.WriteAsync(isReadyBuffer, 0, isReadyBuffer.Length, cancellationToken);

                //получаем ответ от клиента
                string clientResponse = await ReceiveStringAsync(stream, cancellationToken);
                //если ответ disconnect, то выбрасываем исключение об отключении клиента
                if (clientResponse == "disconnect")
                {
                    throw new ClientDisconnectedException();
                }
                //если клиент ответил ready, то отправляем ему длину данных и сами данные
                else
                {
                    await stream.WriteAsync(lengthBytes, 0, lengthBytes.Length, cancellationToken);
                    await stream.FlushAsync(cancellationToken);
                    await stream.WriteAsync(stringBytes, 0, stringBytes.Length, cancellationToken);
                    await stream.FlushAsync(cancellationToken);
                }
            }
            // если мы хотим разорвать соединение
            else if (messageType == "disconnect")
            {
                byte[] disconnectBuffer = Encoding.UTF8.GetBytes("messageType: disconnect;");
                await stream.WriteAsync(disconnectBuffer, 0, disconnectBuffer.Length, cancellationToken);
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
