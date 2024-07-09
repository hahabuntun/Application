using Client.Models;
using Client.Services.Client;
using System.Collections.ObjectModel;

namespace Client.ViewModels
{
    /// <summary>
    /// View модель окна со всеми сообщениями полученным в рамках одной сессии с сервером
    /// </summary>
    public class AllMessagesViewModel : ViewModelBase
    {
        private StoredMessage _selectedMessage; //выбранное сообщение
        private ObservableCollection<StoredMessage> _allMessages = new ObservableCollection<StoredMessage>(); // все отображаемые сообщения


        public ITCPClientService TCPClientService { get; set; }
        public ObservableCollection<StoredMessage> AllMessages { get => _allMessages; }
        public StoredMessage SelectedMessage
        {
            get { return _selectedMessage; }
            set
            {
                _selectedMessage = value;
                OnPropertyChanged();
            }
        }


        /// <summary>
        /// Добавляет сообщение в список всех отображаемых сообщений
        /// </summary>
        /// <param name="message"></param>
        public void AddMessage(Message message)
        {
            StoredMessage mes = new StoredMessage()
            {
                ServerAddress = TCPClientService.ServerAddress,
                ServerPort = TCPClientService.ServerPort,
                Time = message.Time,
                From = message.From,
                Color = message.Color,
                Text = message.Text,
                ImagePath = message.ImagePath
            };
            //если метод вызывался из ui потока
            if (System.Windows.Application.Current.Dispatcher.CheckAccess())
            {
                AllMessages.Add(mes);
            }
            //если метод вызывлся не из ui потока(используем, чтобы избежать ошибок)
            else
            {
                System.Windows.Application.Current.Dispatcher.Invoke(() => AllMessages.Add(mes));
            }
        }
        

        
    }
}
