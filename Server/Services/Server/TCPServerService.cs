﻿using Microsoft.Extensions.Logging;
using Server.Exceptions;
using Server.Helpers;
using Server.Models;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
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
        private bool _isServerStopped = true;
        private bool _isServerRunning = false;
        private string _serverAddress;
        private int _serverPort;
        private Message _message = null;


        public string ServerAddress { get => _serverAddress; set { _serverAddress = value; OnPropertyChanged(); } }
        public int ServerPort { get => _serverPort; set { _serverPort = value; OnPropertyChanged(); } }
        public string ErrorMessage { get => _errorMessage; set { _errorMessage = value; OnPropertyChanged(); } }
        public bool IsServerStopped { get => _isServerStopped; set { _isServerStopped = value; OnPropertyChanged(); } }
        public bool IsServerRunning { get => _isServerRunning; set { _isServerRunning = value; OnPropertyChanged(); } }
        public ObservableCollection<string> AvailableAddresses { get => _availableAddresses; set { _availableAddresses = value; OnPropertyChanged(); } }
        public Message Message { get => _message; set { _message = value; OnPropertyChanged(); } } // отсылаемое сообщение
        private ObservableCollection<TcpClient> _clients = new ObservableCollection<TcpClient>();
        public ObservableCollection<TcpClient> Clients { get => _clients; set { _clients = value; OnPropertyChanged(); } }
        private readonly object _countLock = new object();
        private int _clientsCount;
        public int ClientsCount { get => _clientsCount; set { _clientsCount = value; OnPropertyChanged(); } }

        public event Action<Message, ObservableCollection<TcpClient>> messageSent;
        public CancellationTokenSource MessageFilled { get; set; }



        public TCPServerService(ILogger<TCPServerService> logger)
        {
            _logger = logger;
            random();
        }



        /// <summary>
        /// Вызывается при отсылке сообщения, чтобы обновить окно всех сообщений
        /// </summary>
        public void OnMessageSent(ObservableCollection<TcpClient> clients)
        {
            messageSent?.Invoke(Message, clients);
        }
        /// <summary>
        /// Формирует список доступных ip для сервера
        /// </summary>
        public void random()
        {
            _logger.LogInformation("Вызвана функция random(для плучения всех доступных адрессов");
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
            _logger.LogInformation("Функция random завершила работу");
        }
        

        /// <summary>
        /// Точка входа. Ждет подключения клиентов и начинает их обработку
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public async Task StartServerAsync(CancellationToken cancellationToken)
        {
            MessageFilled = new CancellationTokenSource();
            ErrorMessage = "";
            _logger.LogInformation($"Запуск сервера на {ServerAddress}:{ServerPort}");

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
                    IsServerRunning = true;
                    TcpClient client;
                    _logger.LogInformation("Сервер успешно запущен");

                    while (!cancellationToken.IsCancellationRequested) //пока работа не приостановлена слушаем клиентов
                    {
                        _logger.LogInformation("Ожидаем подключения клиента");
                        client = await tcpListener.AcceptTcpClientAsync(cancellationToken);
                        if (System.Windows.Application.Current.Dispatcher.CheckAccess())
                        {
                            Clients.Add(client);
                            ClientsCount = ClientsCount + 1;
                        }
                        else
                        {
                            _logger.LogInformation("Функция вызвана не из ui потока");
                            System.Windows.Application.Current.Dispatcher.Invoke(() =>
                            {
                                Clients.Add(client);
                                ClientsCount = ClientsCount + 1;
                            });
                        }
                        //await HandleClientAsync(client, cancellationToken); //обрабатываем клиента
                        Task.Run(() => HandleClientAsync(client, cancellationToken), cancellationToken); //обрабатываем клиента       
                    }
                }
            }
            catch(OperationCanceledException)
            {
                IsServerStopped = true;
                IsServerRunning = false;
                Message = null;
                _logger.LogInformation("Server stopped");
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
                IsServerStopped = true;
                IsServerRunning = false;
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
            var ClientAddress = clientEndPoint.Address.ToString();
            var ClientPort = clientEndPoint.Port;
            _logger.LogInformation($"Клиент подключен {ClientAddress}:{ClientPort}");
            //создаем поток для связи с клиентом
            using (NetworkStream stream = client.GetStream())
            {
                try
                {
                    CancellationToken messageFilledToken = MessageFilled.Token;
                    CancellationToken linkedCancellationToken = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, messageFilledToken).Token;
                    try
                    {
                        string disconnect = await ReceiveDisconnectAsync(stream, linkedCancellationToken);
                        throw new ClientDisconnectedException();
                    }
                    catch (OperationCanceledException) when (messageFilledToken.IsCancellationRequested)
                    {
                        _logger.LogInformation("Данные спарсены");
                    }
                    //обработка исключения, когда сработал токен отмены по причине остановки клиента
                    catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                    {
                        throw;
                    }
     

                    _logger.LogInformation("Отправляем данные клиенту");
                    await SendMessageAsync(stream, Message, cancellationToken); //Отправляем обработанные данные клиенту
                    _logger.LogInformation("Данные были отправлены");
                    _logger.LogInformation("Оповещаем подписчиков об отправке сообщения");

                    string clientRequest;
                    //ждем запроса на пересылку данных или на выключение
                    while (!cancellationToken.IsCancellationRequested) //пока сервер не остановлен
                    {
                        clientRequest = await ReceiveStringAsync(stream, cancellationToken); //слушаем запросы от клиента
                        if (clientRequest == "resend") //если клиент запросил resend
                        {
                            _logger.LogInformation("Отправляем данные клиенту");
                            await SendMessageAsync(stream, Message, cancellationToken); //отправляем ему сообщение повторно
                            _logger.LogInformation("Данные были отправлены");
                            _logger.LogInformation("Оповещаем подписчиков об отправке сообщения");
                            OnMessageSent(new ObservableCollection<TcpClient>() { client });
                        }
                        else if (clientRequest == "disconnect") //если клиент отправил запрос disconnect
                        {
                            _logger.LogInformation("Клиент отправил запрос disconnect");
                            throw new ClientDisconnectedException(); //выбрасываем исключение об отключении клиента
                        }
                    }
                    _logger.LogInformation("Отправляем строку disconnect");
                    await SendStringAsync(stream, "disconnect", "", CancellationToken.None); //отправляем строку disconnect(нельзя отменить операцию)
                    _logger.LogInformation("Строка отправлена");
                }
                catch (OperationCanceledException)
                {
                    _logger.LogInformation("Отправляем строку disconnect");
                    await SendStringAsync(stream, "disconnect", "", CancellationToken.None); //отправляем строку disconnect(нельзя отменить операцию)
                    client?.Close();
                    _logger.LogInformation("Строка отправлена");
                    throw;
                }
                catch(ClientDisconnectedException)
                {
                    _logger.LogWarning("Client sent disconnect string");
                }
                catch(Exception)
                {
                    _logger.LogWarning("ErrorOccured");
                }
                finally
                {
                    if (System.Windows.Application.Current.Dispatcher.CheckAccess())
                    {
                        Clients?.Remove(client);
                        lock (_countLock)
                        {
                            ClientsCount = ClientsCount - 1;
                        }
                    }
                    else
                    {
                        System.Windows.Application.Current.Dispatcher.Invoke(() => { 
                            Clients?.Remove(client);
                            lock (_countLock)
                            {
                                ClientsCount = ClientsCount - 1;
                            }
                        });
                    }
                    client?.Close();
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
            _logger.LogInformation("Ожидаем получение строки от клиента");
            byte[] requestBytes = new byte[1024];
            string request;
            int bytesRead;
            bytesRead = await stream.ReadAsync(requestBytes, 0, requestBytes.Length, cancellationToken); //получаем данные от клиента
            request = Encoding.UTF8.GetString(requestBytes, 0, bytesRead);
            _logger.LogInformation($"Строка получена: {request}");
            return request;
        }

        /// <summary>
        /// Получает запрос отключения от клиента
        /// </summary>
        /// <param name="stream"></param>
        /// <param name="linkedCancellationToken"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public async Task<string> ReceiveDisconnectAsync(NetworkStream stream, CancellationToken linkedCancellationToken)
        {
            _logger.LogInformation("Ожидаем получения сообщения об отключении клиента или получение данных для отправки");
            byte[] requestBuffer = new byte[64];
            int bytes_read = await stream.ReadAsync(requestBuffer, 0, 64, linkedCancellationToken); //ждем сообщенеия от сервера
            _logger.LogInformation("Получено сообщение");

            //выделяем данные из полученного сообщения
            string request = Encoding.UTF8.GetString(requestBuffer, 0, bytes_read);
            //если сервер не отправил строку disconnect то выбрасываем ошибку, потому что эта строка не ожидается
            if (request != "disconnect")
            {
                _logger.LogError($"Получено неожиданное сообщение: {request}");
                throw new Exception("Клиент отправил неожиданное сообщение");
            }
            _logger.LogInformation("Получено сообщение disconnect");
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
            _logger.LogInformation("Отправляем text");
            //отправляем текст
            if (message.Text != null && message.Text != "")
                await SendStringAsync(stream, "is-ready-to-rec", message.Text, cancellationToken);
            else
                await SendStringAsync(stream, "is-ready-to-rec", "no-data", cancellationToken);
            _logger.LogInformation("Отправляем color");
            //отправляем цвет
            if (message.Color != null && message.Color != "")
                await SendStringAsync(stream, "is-ready-to-rec", message.Color, cancellationToken);
            else
                await SendStringAsync(stream, "is-ready-to-rec", "no-data", cancellationToken);
            _logger.LogInformation("Отправляем from");
            //отправляем From
            if (message.From != null && message.From != "")
                await SendStringAsync(stream, "is-ready-to-rec", message.From, cancellationToken);
            else
                await SendStringAsync(stream, "is-ready-to-rec", "no-data", cancellationToken);
            _logger.LogInformation("Отправляем картинку");
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
            _logger.LogInformation("Отправляем messageType: is-ready-to-rec;");
            //отправляем сообщение, с проверкой готовности получения данных клиентом
            byte[] isReadyBuffer = Encoding.UTF8.GetBytes("messageType: is-ready-to-rec;");
            await stream.WriteAsync(isReadyBuffer, 0, isReadyBuffer.Length, cancellationToken);
            _logger.LogInformation("Отправлено");
            _logger.LogInformation("Ожидаем ответа от клиента");
            string clientResponse = await ReceiveStringAsync(stream, cancellationToken); //получаем ответ от клиента
            //если ответ ready, то отправляем длину данных, а затем сами данные
            if (clientResponse == "ready")
            {
                _logger.LogInformation("Отправляем длину картинки");
                byte[] imageLengthBytes = BitConverter.GetBytes(image.Length);
                await stream.WriteAsync(imageLengthBytes, 0, imageLengthBytes.Length, cancellationToken);
                await stream.FlushAsync(cancellationToken);
                _logger.LogInformation("Длина картинки отправлена картинку");
                _logger.LogInformation("Отправляем картинку");
                await stream.WriteAsync(image, 0, image.Length, cancellationToken);
                await stream.FlushAsync(cancellationToken);
                _logger.LogInformation("Картинка отправлена");
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
                _logger.LogInformation("Отправляем messageType: is-ready-to-rec;");
                await stream.WriteAsync(isReadyBuffer, 0, isReadyBuffer.Length, cancellationToken);
                _logger.LogInformation("Отправлено");
                _logger.LogInformation("Ожидаем ответа от клиента");
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
                    _logger.LogInformation("Отправляем клиенту длину строки");
                    await stream.WriteAsync(lengthBytes, 0, lengthBytes.Length, cancellationToken);
                    await stream.FlushAsync(cancellationToken);
                    _logger.LogInformation("Длина строки отправлена");
                    _logger.LogInformation("Отправляем клиенту строку");
                    await stream.WriteAsync(stringBytes, 0, stringBytes.Length, cancellationToken);
                    await stream.FlushAsync(cancellationToken);
                    _logger.LogInformation("Строка отправлена");
                }
            }
            // если мы хотим разорвать соединение
            else if (messageType == "disconnect")
            {
                _logger.LogInformation("Отправляем messageType: disconnect;");
                byte[] disconnectBuffer = Encoding.UTF8.GetBytes("messageType: disconnect;");
                await stream.WriteAsync(disconnectBuffer, 0, disconnectBuffer.Length, cancellationToken);
                _logger.LogInformation("Строка messageType: disconnect; отправлена");
            }
        }


        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
