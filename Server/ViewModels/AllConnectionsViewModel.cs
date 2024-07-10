using Microsoft.Extensions.Logging;
using Server.Commands;
using Server.Services;
using Server.Services.Server;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Input;

namespace Server.ViewModels
{
    public class AllConnectionsViewModel : ViewModelBase, INotifyPropertyChanged
    {
        private readonly ILogger<AllConnectionsViewModel> _logger;
        private readonly IWindowManagerService _windowManagerService;
        private readonly ViewModelLocatorService _viewModelLocatorService;
        private ObservableCollection<SingleConnectionViewModel> _singleConnectionViewModels = new ObservableCollection<SingleConnectionViewModel>();
        private SingleConnectionViewModel _selectedConnectionViewModel;


        public ObservableCollection<SingleConnectionViewModel> SingleConnectionViewModels { get => _singleConnectionViewModels; set { _singleConnectionViewModels = value; } }
        public SingleConnectionViewModel SelectedConnectionViewModel
        {
            get => _selectedConnectionViewModel;
            set { _selectedConnectionViewModel = value; OnPropertyChanged(); }
        }


        public ICommand CloseSubWindowsCommand { get;  } //команда закрытия всех подокон
        public ICommand CloseSelfCommand { get;  } //команда закрытия себя
        public ICommand CreateSubWindowCommand { get; } //команда создания подокон
        public ICommand ActivateSubWindowCommand { get; } //команда вывода подокон на передний план
        public ICommand CloseSubWindowCommand { get; } //команда закрытия одного подокна
        
        
        public AllConnectionsViewModel(ILogger<AllConnectionsViewModel> logger, IWindowManagerService windowManagerService, ViewModelLocatorService viewModelLocatorService)
        {
            _logger = logger;
            _windowManagerService = windowManagerService;
            _viewModelLocatorService = viewModelLocatorService;
            CloseSelfCommand = new RelayCommand(param => CloseSelf());
            CloseSubWindowsCommand = new RelayCommand(param => ClearResources());
            CloseSubWindowCommand = new RelayCommand(param => CloseSubWindow((SingleConnectionViewModel)param));
            ActivateSubWindowCommand = new RelayCommand(param => ActivateSubWindow((SingleConnectionViewModel)param));
            CreateSubWindowCommand = new RelayCommand(param => CreateSubWindow());
        }
        
        
        /// <summary>
        /// Создание подокна.
        /// </summary>
        public void CreateSubWindow()
        {
            _logger.LogInformation("Вызвана команда открытия окна соединения");
            SingleConnectionViewModel singleConnectionViewModel =  _viewModelLocatorService.SingleConnectionViewModel;
            _windowManagerService.ShowWindow(singleConnectionViewModel);
            SingleConnectionViewModels.Add(singleConnectionViewModel);
        }


        /// <summary>
        /// Закрытие одного подокна
        /// </summary>
        /// <param name="viewModelToClose"></param>
        public void CloseSubWindow(SingleConnectionViewModel viewModelToClose)
        {
            _logger.LogInformation("Вызвана команда закрытия окна соединения");
            if (viewModelToClose == null)
                return;
            SingleConnectionViewModels.Remove(viewModelToClose);
            viewModelToClose.CloseAction?.Invoke(); 
        }


        /// <summary>
        /// Вывод подокна на передний план
        /// </summary>
        /// <param name="viewModelToActivate"></param>
        public void ActivateSubWindow(SingleConnectionViewModel viewModelToActivate)
        {
            _logger.LogInformation("Вызвана команда активации(вывода на передний план) окна соединения");
            if (viewModelToActivate == null)
                return;
            foreach (var viewModel in SingleConnectionViewModels)
            {
                if (viewModel == viewModelToActivate)
                {
                    viewModelToActivate.ActivateAction?.Invoke();
                    return;
                }
            }
        }


        /// <summary>
        /// Очищение ресурсов окна при закрытии
        /// </summary>
        public override void ClearResources()
        {
            _logger.LogInformation("Вызвана очистка ресурсов");
            foreach (var viewModel in SingleConnectionViewModels)
            {
                viewModel.CloseAction?.Invoke();
            }
            SingleConnectionViewModels.Clear();
        }
    }
}
