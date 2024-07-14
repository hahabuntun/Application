using Microsoft.Extensions.Logging;
using Server.Models;
using Server.Services.Server;
using System.Collections.ObjectModel;
using System.Net;
using System.Net.Sockets;


namespace Server.ViewModels
{
    /// <summary>
    /// Отображает все сообщения за одну пользовательскую сессию
    /// </summary>
    public class AllMessagesViewModel : ViewModelBase
    {
        private readonly ILogger<AllMessagesViewModel> _logger;
        private StoredMessage _selectedMessage;
        public StoredMessage SelectedMessage
        {
            get { return _selectedMessage; }
            set
            {
                _selectedMessage = value;
                OnPropertyChanged();
            }
        }
        public ITCPServerService TCPServerService { get; set; }
        public ObservableCollection<StoredMessage> _allMessages = new ObservableCollection<StoredMessage>();
        public ObservableCollection<StoredMessage> AllMessages { get => _allMessages; }


        public AllMessagesViewModel(ILogger<AllMessagesViewModel> logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// Добавление сообщения
        /// </summary>
        /// <param name="message"></param>
        public void AddMessage(Message message, TcpClient client)
        {
            _logger.LogInformation("Вызвана функция добавления сообщения в список всех сообщений");
            IPEndPoint? clientEndPoint = client.Client.RemoteEndPoint as IPEndPoint;
            var ClientAddress = clientEndPoint.Address.ToString();
            var ClientPort = clientEndPoint.Port;
            StoredMessage mes = new StoredMessage()
            {
                ClientAddress = ClientAddress,
                ClientPort = ClientPort,
                ServerAddress = TCPServerService.ServerAddress,
                ServerPort = TCPServerService.ServerPort,
                Time=DateTime.Now,
                Id = message.Id,
                FormatVersion = message.FormatVersion,
                From = message.From,
                To = message.To,
                Color = message.Color,
                Text = message.Text,
                ImagePath = message.ImagePath
            };
            //если текущий поток является UI потоком
            if (System.Windows.Application.Current.Dispatcher.CheckAccess())
            {
                _logger.LogInformation("Функция вызвана из ui потока");
                AllMessages.Add(mes);
            }
            //если текущий поток не является UI потоком(во избежание ошибки)
            else
            {
                _logger.LogInformation("Функция вызвана не из ui потока");
                System.Windows.Application.Current.Dispatcher.Invoke(() => AllMessages.Add(mes));
            }
            _logger.LogInformation("Функция отработала");
        }


    }

}
