

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
        public ITCPServerService TCPServerService { get; set; }

        public ObservableCollection<StoredMessage> AllMessages => TCPServerService?.AllMessages;
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
