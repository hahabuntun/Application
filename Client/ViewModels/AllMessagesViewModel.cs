
using Client.Models;
using Client.Services.Client;
using System.Collections.ObjectModel;

namespace Client.ViewModels
{
    public class AllMessagesViewModel : ViewModelBase
    {
        public ITCPClientService TCPClientService { get; set; }
        public ObservableCollection<StoredMessage> _allMessages = new ObservableCollection<StoredMessage>();
        public ObservableCollection<StoredMessage> AllMessages { get => _allMessages; }
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
            if (System.Windows.Application.Current.Dispatcher.CheckAccess())
            {
                AllMessages.Add(mes);
            }
            else
            {
                System.Windows.Application.Current.Dispatcher.Invoke(() => AllMessages.Add(mes));
            }
        }
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
    }
}
