

using Client.Commands;
using Client.Services.Client;
using System.Windows.Input;

namespace Client.ViewModels
{
    public class ClientViewModel : ViewModelBase
    {
        private readonly ITCPClientService _tcpClientService;
        private CancellationTokenSource _tokenSource;
        public ITCPClientService TcpClientService { get => _tcpClientService; }
        public ICommand StopClientCommand { get; }
        public ICommand StartClientCommand { get; }
        public ICommand RequestResendCommand { get; }
        public ICommand CloseSelfCommand { get; }
        public ClientViewModel(ITCPClientService tcpClientService)
        {
            _tcpClientService = tcpClientService;
            StartClientCommand = new RelayCommand((param) => StartClient(), (param)=> _tcpClientService.IsConnected == false);
            StopClientCommand = new RelayCommand((param) => StopClient(), (param) => _tcpClientService.IsConnected == true);
            RequestResendCommand = new RelayCommand((param) => RequestResend(), (param) => _tcpClientService.Message != null);
        }
        public void StartClient()
        {
            _tokenSource = new CancellationTokenSource();
            CancellationToken cancellationToken = _tokenSource.Token;
            Task.Run(() => _tcpClientService.StartClientAsync(cancellationToken), cancellationToken);
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
            StopClient();
        }
    }
}
