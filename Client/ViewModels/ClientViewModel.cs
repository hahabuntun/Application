

using Client.Commands;
using Client.Services;
using Client.Services.Client;
using System.Windows.Input;

namespace Client.ViewModels
{
    public class ClientViewModel : ViewModelBase
    {
        private readonly ITCPClientService _tcpClientService;
        private CancellationTokenSource _tokenSource;
        private readonly IWindowManagerService _windowManagerService;
        private readonly ViewModelLocatorService _viewModelLocatorService;
        private AllMessagesViewModel _allMessagesViewModel;
        public ITCPClientService TcpClientService { get => _tcpClientService; }
        public ICommand StopClientCommand { get; }
        public ICommand StartClientCommand { get; }
        public ICommand RequestResendCommand { get; }
        public ICommand CloseSelfCommand { get; }
        public ICommand OpenAllMessagesCommand { get; }
        public ClientViewModel(ITCPClientService tcpClientService, IWindowManagerService windowManagerService, ViewModelLocatorService viewModelLocatorService)
        {
            _windowManagerService = windowManagerService;
            _viewModelLocatorService = viewModelLocatorService;
            _allMessagesViewModel = viewModelLocatorService.AllMessagesViewModel;
            _tcpClientService = tcpClientService;
            _tcpClientService.messageReceived += _allMessagesViewModel.AddMessage;
            _allMessagesViewModel.TCPClientService = _tcpClientService;
            StartClientCommand = new RelayCommand((param) => StartClient(), (param)=> _tcpClientService.IsConnected == false);
            StopClientCommand = new RelayCommand((param) => StopClient(), (param) => _tcpClientService.IsConnected == true);
            RequestResendCommand = new RelayCommand((param) => RequestResend(), (param) => _tcpClientService.Message != null);
            OpenAllMessagesCommand = new RelayCommand((param) => OpenAllMessages(), (param) => true);
        }
        public void StartClient()
        {
            _tokenSource = new CancellationTokenSource();
            CancellationToken cancellationToken = _tokenSource.Token;
            Task.Run(() => _tcpClientService.StartClientAsync(cancellationToken), cancellationToken);
        }
        public void OpenAllMessages()
        {
            _windowManagerService.ShowWindow(_allMessagesViewModel);
        }
        public void StopClient()
        {
            _tokenSource?.Cancel();
        }
        public void RequestResend()
        {
            _tcpClientService.ResendCancellationTokenSource?.Cancel();
        }
        public override void ClearResources()
        {
            _allMessagesViewModel?.CloseAction?.Invoke();
            StopClient();
        }
    }
}
