using Client.Exceptions;
using Client.Models;
using Microsoft.Extensions.Logging;
using System.ComponentModel;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Text;

namespace Client.Services.Client
{
    public class TCPClientService : ITCPClientService, INotifyPropertyChanged
    {
        private readonly ILogger<TCPClientService> _logger;
        private string _serverAddress;
        private int _serverPort;
        private string _clientAddress;
        private int _clientPort;
        private bool _isConnected;
        private string _connectionStatus = "Not Connected";
        private string _errorMessage;
        private Message _message;


        public string ServerAddress { get => _serverAddress; set { _serverAddress = value; OnPropertyChanged(); } }
        public int ServerPort { get => _serverPort; set { _serverPort = value; OnPropertyChanged(); } }
        public string ClientAddress { get => _clientAddress; set { _clientAddress = value; OnPropertyChanged(); } }
        public int ClientPort { get => _clientPort; set { _clientPort = value; OnPropertyChanged(); } }
        public bool IsConnected { get => _isConnected; set { _isConnected = value; OnPropertyChanged(); } }
        public Message Message { get => _message; set { _message = value; OnPropertyChanged(); } }
        public string ConnectionStatus { get => _connectionStatus; set { _connectionStatus = value; OnPropertyChanged(); } }
        public string ErrorMessage { get => _errorMessage; set { _errorMessage = value; OnPropertyChanged(); } }
        public CancellationTokenSource ResendCancellationTokenSource { get; set; }
       
       
        public TCPClientService(ILogger<TCPClientService> logger)
        {
            _logger = logger;
        }


        public event Action<Message> messageReceived;
        public void OnMessageReceived()
        {
            messageReceived?.Invoke(Message);
        }


        /// <summary>
        /// Точка входа для связи с сервером
        /// Создается подключение к серверу => принимаются данные от сервера => отправляется запрос на повторную отправку серверу(когда нажата кнопка)
        /// По отмене операции отправляет сообщение disconnect серверу
        /// По приходе сообщения disconnect от севера закрывает подключение
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public async Task StartClientAsync(CancellationToken cancellationToken)
        {
            //обнуляем данные для отображения
            _logger.LogInformation("Starting client...");
            ErrorMessage = "";
            ConnectionStatus = "Not Connected";
            IsConnected = false;
            Message = null;
            ClientPort = 0;
            ClientAddress = "";
            try
            {
                using (TcpClient tcpClient = new TcpClient())
                {
                    _logger.LogInformation("Connecting to server at {ServerAddress}:{ServerPort}", ServerAddress, ServerPort);
                    await tcpClient.ConnectAsync(ServerAddress, ServerPort, cancellationToken); //подключение к серверу
                    IsConnected = true;
                    ConnectionStatus = "Connected";
                    _logger.LogInformation("Connected to server at {ServerAddress}:{ServerPort}", ServerAddress, ServerPort);

                    IPEndPoint clientEndPoint = tcpClient.Client.LocalEndPoint as IPEndPoint; //получаем свои данные(адресс и port)
                    if (clientEndPoint != null)
                    {
                        ClientAddress = clientEndPoint.Address.ToString();
                        ClientPort = clientEndPoint.Port;
                        _logger.LogInformation("Client endpoint is {ClientAddress}:{ClientPort}", ClientAddress, ClientPort);
                    }

                    using (NetworkStream stream = tcpClient.GetStream()) //получаем поток для обмена данных с сервером
                    {
                        while (!cancellationToken.IsCancellationRequested) // пока сервер работает(пока работу не прервала viewModel)
                        {
                            //добавляем дополнительный токен отмены(сработает, если мы запросим повторное получение данных)
                            ResendCancellationTokenSource = new CancellationTokenSource(); 
                            CancellationToken resendToken = ResendCancellationTokenSource.Token;
                            CancellationToken linkedCancellationToken = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, resendToken).Token;
                            try
                            {
                                _logger.LogInformation("Receiving message from server...");
                                Message = await ReceiveMessage(stream, cancellationToken); //получаем данные от сервера
                                OnMessageReceived();
                                _logger.LogInformation("Message received from server.");
                                // ждем сообщения отключения от сервера
                                // если клиент прерывает работу то срабатывает обычный токен отмены, если запрошена повторная отправка данных, то сработает второй токен
                                string discString = await ReceiveDisconnectAsync(stream, linkedCancellationToken); 
                                
                                _logger.LogWarning("Client disconnected by server.");
                                break;
                            }
                            //обработка исплючения, когда сработал токен отмены по причине повторного запроса
                            catch (OperationCanceledException) when (resendToken.IsCancellationRequested)
                            {
                                Message = null;
                                await Task.Delay(1000);
                                _logger.LogWarning("Resend requested");
                                await SendStringAsync(stream, "resend", cancellationToken);
                            }
                            //обработка исключения, когда сработал токен отмены по причине остановки клиента
                            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                            {
                                    _logger.LogWarning("Disconnection requested.");
                                    await SendStringAsync(stream, "disconnect", CancellationToken.None);
                                    break;
                            }
                        }
                    }
                }
            }
            //обработка исключения(сервре завершает работу)
            catch (ServerDisconnectedException)
            {
                _logger.LogWarning("Client disconnected by server.");
            }
            //другие исключения
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while communicating with the server.");
                ErrorMessage = ex.Message;
            }
            //очистка используемых данных и обновление ui
            finally
            {
                ResendCancellationTokenSource = null;
                ConnectionStatus = "Not Connected";
                IsConnected = false;
                Message = null;
                ClientPort = 0;
                ClientAddress = "";
                _logger.LogInformation("Client stopped.");
            }
        }


        /// <summary>
        /// Принимает данные от сервера
        /// </summary>
        /// <param name="stream"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public async Task<Message> ReceiveMessage(NetworkStream stream, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Receiving text...");
            string text = await ReceiveStringAsync(stream, cancellationToken); // получение текста от сервера
            _logger.LogInformation("Receiving color...");
            string color = await ReceiveStringAsync(stream, cancellationToken); // получение цвета от сервера
            _logger.LogInformation("Receiving sender info...");
            string from = await ReceiveStringAsync(stream, cancellationToken); // получение from от сервера
            _logger.LogInformation("Receiving image...");
            byte[] image = await ReceiveImageAsync(stream, cancellationToken); // получение картинки от сервера
            _logger.LogInformation("Saving image to disk...");
            string imagePath = await SaveImageToDiskAsync(image, cancellationToken); // сохранение картинки
            //сохраняем данные полученные от сервера в переменную mes
            Message mes = new Message()
            {
                From = from,
                Text = text,
                Color = color,
                ImagePath = imagePath
            };
            return mes;
        }


        /// <summary>
        /// Принимает картинку от сервера
        /// Сначала принимает запрос is-ready-to-rec от сервера и отвечает на него ready
        /// Затем принимает длину картинки и потом саму картинку
        /// </summary>
        /// <param name="stream"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        /// <exception cref="ServerDisconnectedException"></exception>
        public async Task<byte[]> ReceiveImageAsync(NetworkStream stream, CancellationToken cancellationToken)
        {
            byte[] imageBuffer = [];

            _logger.LogInformation("Waiting for server readiness to send image...");
            byte[] isReadyBuffer = new byte[64];
            await stream.ReadAsync(isReadyBuffer, 0, 64, cancellationToken); // ожидаем запроса messageType: is-ready-to-rec; от сервера
            
            //выделяем данные из полученного сообщения
            string isReadyMessage = Encoding.UTF8.GetString(isReadyBuffer);
            int spaceIndex = isReadyMessage.IndexOf(' ');
            int semicolonIndex = isReadyMessage.IndexOf(';');

            string messageType = isReadyMessage.Substring(spaceIndex + 1, semicolonIndex - spaceIndex - 1);
            //если получено ожидаемое сообщение то получаем сначала размер данных, а затем сами данные
            if (messageType == "is-ready-to-rec")
            {
                await SendStringAsync(stream, "ready", cancellationToken); //отправляем строку ready на сервер
                byte[] lengthBuffer = new byte[4]; 
                await stream.ReadAsync(lengthBuffer, 0, 4, cancellationToken); //ждем размера картинки
                int length = BitConverter.ToInt32(lengthBuffer, 0);
                
                //считываем картинку
                imageBuffer = new byte[length];
                int totalBytesRead = 0;
                while (totalBytesRead < length)
                {
                    int bytesRead = await stream.ReadAsync(imageBuffer, totalBytesRead, length - totalBytesRead, cancellationToken);
                    totalBytesRead += bytesRead;
                }
                _logger.LogInformation("Image received.");
            }
            //если сервер отправил строку disconnect
            else if (messageType == "disconnect")
            {
                _logger.LogWarning("Server sent disconnect message.");
                throw new ServerDisconnectedException();
            }

            return imageBuffer;
        }


        /// <summary>
        /// Принимает строку от сервера
        /// Сначала принимает запрос is-ready-to-rec на который отвечает ready
        /// Затем принимает длину строки и саму строку
        /// </summary>
        /// <param name="stream"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        /// <exception cref="ServerDisconnectedException"></exception>
        /// <exception cref="Exception"></exception>
        public async Task<string> ReceiveStringAsync(NetworkStream stream, CancellationToken cancellationToken)
        {
            string receivedString = "";

            _logger.LogInformation("Waiting for server readiness to send string...");
            byte[] isReadyBuffer = new byte[64];
            await stream.ReadAsync(isReadyBuffer, 0, 64, cancellationToken); // ожидаем запроса messageType: is-ready-to-rec; от сервера
            
            //выделяем данные из полученного сообщения
            string isReadyMessage = Encoding.UTF8.GetString(isReadyBuffer); 
            int spaceIndex = isReadyMessage.IndexOf(' ');
            int semicolonIndex = isReadyMessage.IndexOf(';');

            //если получено ожидаемое сообщение то получаем сначала размер данных, а затем сами данны
            string messageType = isReadyMessage.Substring(spaceIndex + 1, semicolonIndex - spaceIndex - 1);
            if (messageType == "is-ready-to-rec")
            {
                await SendStringAsync(stream, "ready", cancellationToken); //отправляем строку ready на сервер
                byte[] lengthBuffer = new byte[4];
                await stream.ReadAsync(lengthBuffer, 0, 4, cancellationToken); //ждем размера сообщения
                int length = BitConverter.ToInt32(lengthBuffer, 0);

                byte[] stringBuffer = new byte[length];
                await stream.ReadAsync(stringBuffer, 0, length, cancellationToken); //получаем сообщение
                receivedString = Encoding.UTF8.GetString(stringBuffer);
                _logger.LogInformation("String received.");

                return receivedString;
            }
            //если сервер отправил строку disconnect
            else if (messageType == "disconnect")
            {
                _logger.LogWarning("Server sent disconnect message.");
                throw new ServerDisconnectedException();

            }
            throw new Exception("Server sent unexpected message");
        }


        /// <summary>
        /// Ждет прихода запроса disconnect от сервера
        /// Эта функция вызывается когда клиент уже получил данные от сервера и ждет, когда придет команда resend
        /// Если она пришла то надо прекратить работу этой функции
        /// Если сервер остановлен то тоже надо остановить работу этой функции
        /// В эту функцию передается объединенный токен отмены(для условий приведенных выше)
        /// Вызывающий код должен понять какой из этих токенов вызывал операцию(пока не работает)
        /// </summary>
        /// <param name="stream"></param>
        /// <param name="linkedCancellationToken"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public async Task<string> ReceiveDisconnectAsync(NetworkStream stream, CancellationToken linkedCancellationToken)
        {
            _logger.LogInformation("Waiting for server disconnect message...");
            byte[] isReadyBuffer = new byte[64];
            await stream.ReadAsync(isReadyBuffer, 0, 64, linkedCancellationToken); //ждем сообщенеия от сервера
            
            //выделяем данные из полученного сообщения
            string isReadyMessage = Encoding.UTF8.GetString(isReadyBuffer);
            int spaceIndex = isReadyMessage.IndexOf(' ');
            int semicolonIndex = isReadyMessage.IndexOf(';');
            string messageType = isReadyMessage.Substring(spaceIndex + 1, semicolonIndex - spaceIndex - 1);

            //если сервер не отправил строку disconnect то выбрасываем ошибку, потому что эта строка не ожидается
            if (messageType != "disconnect")
            {
                _logger.LogError("Unexpected message type received: {MessageType}", messageType);
                throw new Exception("Server sent unexpected message;");
            }
            _logger.LogInformation("Server disconnect message received.");
            return messageType;
        }


        /// <summary>
        /// Отправляет строку на сервер
        /// </summary>
        /// <param name="stream"></param>
        /// <param name="message"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public async Task SendStringAsync(NetworkStream stream, string message, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Sending string to server: {Message}", message);
            byte[] data = Encoding.UTF8.GetBytes(message);
            await stream.WriteAsync(data, cancellationToken);
            await stream.FlushAsync(cancellationToken);
            _logger.LogInformation("String sent to server.");
        }


        /// <summary>
        /// Сохраняет картинку на диск
        /// </summary>
        /// <param name="imageData"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        private async Task<string> SaveImageToDiskAsync(byte[] imageData, CancellationToken cancellationToken)
        {
            string fileName = $"{Guid.NewGuid()}.bmp";
            string filePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), fileName);
            _logger.LogInformation("Saving image to disk at {FilePath}", filePath);
            await File.WriteAllBytesAsync(filePath, imageData, cancellationToken);
            _logger.LogInformation("Image saved to disk at {FilePath}", filePath);
            return filePath;
        }


        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
