using Server.Commands;
using Server.Services;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Input;

namespace Server.ViewModels
{
    public class AllConnectionsViewModel : ViewModelBase, INotifyPropertyChanged
    {
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
        public ICommand CloseSubWindowsCommand { get;  }
        public ICommand CloseSelfCommand { get;  }
        public ICommand CreateSubWindowCommand { get; }
        public ICommand ActivateSubWindowCommand { get; }
        public ICommand CloseSubWindowCommand { get; }
        public AllConnectionsViewModel(IWindowManagerService windowManagerService, ViewModelLocatorService viewModelLocatorService)
        {
            _windowManagerService = windowManagerService;
            _viewModelLocatorService = viewModelLocatorService;
            CloseSelfCommand = new RelayCommand(param => CloseSelf());
            CloseSubWindowsCommand = new RelayCommand(param => ClearResources());
            CloseSubWindowCommand = new RelayCommand(param => CloseSubWindow((SingleConnectionViewModel)param));
            ActivateSubWindowCommand = new RelayCommand(param => ActivateSubWindow((SingleConnectionViewModel)param));
            CreateSubWindowCommand = new RelayCommand(param => CreateSubWindow());
        }
        public void CreateSubWindow()
        {
            SingleConnectionViewModel singleConnectionViewModel =  _viewModelLocatorService.SingleConnectionViewModel;
            _windowManagerService.ShowWindow(singleConnectionViewModel);
            SingleConnectionViewModels.Add(singleConnectionViewModel);
        }
        public void CloseSubWindow(SingleConnectionViewModel viewModelToClose)
        {
            if (viewModelToClose == null)
                return;
            SingleConnectionViewModels.Remove(viewModelToClose);
            viewModelToClose.CloseAction?.Invoke();
            
            
        }
        public void ActivateSubWindow(SingleConnectionViewModel viewModelToActivate)
        {
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
        public override void ClearResources()
        {
            foreach (var viewModel in SingleConnectionViewModels)
            {
                viewModel.CloseAction?.Invoke();
            }
            SingleConnectionViewModels.Clear();
        }
    }
}
