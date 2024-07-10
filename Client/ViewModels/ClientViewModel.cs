using Client.Commands;
using Client.Services;
using Client.Services.Client;
using Microsoft.Extensions.Logging;
using System.Windows.Input;

namespace Client.ViewModels
{
    public class ClientViewModel : ViewModelBase
    {
        private readonly ILogger<ClientViewModel> _logger;
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


        public ClientViewModel(ILogger<ClientViewModel> logger, ITCPClientService tcpClientService, IWindowManagerService windowManagerService, ViewModelLocatorService viewModelLocatorService)
        {
            _logger = logger;
            _windowManagerService = windowManagerService;
            _viewModelLocatorService = viewModelLocatorService;
            _allMessagesViewModel = viewModelLocatorService.AllMessagesViewModel;
            _tcpClientService = tcpClientService;
            _tcpClientService.messageReceived += _allMessagesViewModel.AddMessage; //добавляем подписчика к событию получения данных от сервера
            _allMessagesViewModel.TCPClientService = _tcpClientService;
            //инициализируем комманды
            StartClientCommand = new RelayCommand((param) => StartClient(), (param)=> _tcpClientService.IsConnected == false);
            StopClientCommand = new RelayCommand((param) => StopClient(), (param) => _tcpClientService.IsConnected == true);
            RequestResendCommand = new RelayCommand((param) => RequestResend(), (param) => _tcpClientService.Message != null);
            OpenAllMessagesCommand = new RelayCommand((param) => OpenAllMessages(), (param) => true);
        }


        /// <summary>
        /// Подключаемся к серверу и начинаем обмен сообщениями
        /// </summary>
        public void StartClient()
        {
            _logger.LogInformation("Вызвана команда старт");
            _tokenSource = new CancellationTokenSource();
            CancellationToken cancellationToken = _tokenSource.Token;
            Task.Run(() => _tcpClientService.StartClientAsync(cancellationToken), cancellationToken);
        }


        /// <summary>
        /// Открываем окно со всеми сообщениями сессии
        /// </summary>
        public void OpenAllMessages()
        {
            _logger.LogInformation("Вызвана команда открытия страницы всех сообщений");
            _windowManagerService.ShowWindow(_allMessagesViewModel);
        }


        /// <summary>
        /// Приостанавливаем работу клиента
        /// </summary>
        public void StopClient()
        {
            _logger.LogInformation("Вызвана команда остановки клиента");
            _tokenSource?.Cancel();
        }


        /// <summary>
        /// Запрашиваем сообщение повторно
        /// </summary>
        public void RequestResend()
        {
            _logger.LogInformation("Вызвана команда повторной отправки");
            _tcpClientService.ResendCancellationTokenSource?.Cancel();
        }


        /// <summary>
        /// Очищаем ресурсы
        /// и закрываем окно, которое привязано к allMessagesViewModel
        /// </summary>
        public override void ClearResources()
        {
            _logger.LogInformation("Вызвана функция очищения ресурсов");
            _allMessagesViewModel?.CloseAction?.Invoke();
            StopClient();
        }
    }
}
