using Server.Models;
using Server.Services.Server;
using System.Collections.ObjectModel;


namespace Server.ViewModels
{
    /// <summary>
    /// Отображает все сообщения за одну пользовательскую сессию
    /// </summary>
    public class AllMessagesViewModel : ViewModelBase
    {
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


        /// <summary>
        /// Добавление сообщения
        /// </summary>
        /// <param name="message"></param>
        public void AddMessage(Message message)
        {
            StoredMessage mes = new StoredMessage()
            {
                ServerAddress = TCPServerService.ServerAddress,
                ClientAddress = TCPServerService.ClientAddress,
                ServerPort = TCPServerService.ServerPort,
                ClientPort = TCPServerService.ClientPort,
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
                AllMessages.Add(mes);
            }
            //если текущий поток не является UI потоком(во избежание ошибки)
            else
            {
                System.Windows.Application.Current.Dispatcher.Invoke(() => AllMessages.Add(mes));
            }
        }
        

    }

}
