using Client.ViewModels;
using System.Windows;

namespace Client.Services
{

    public interface IWindowManagerService
    {
        void ShowWindow(ViewModelBase viewModel);
        void CloseWindow(Window window);
    }

    /// <summary>
    /// Класс управления окнами
    /// Содержит основные настройки открытия и закрытия окон
    /// </summary>
    public class WindowManagerService : IWindowManagerService
    {
        private readonly WindowMapperService _windowMapperService;


        public WindowManagerService(WindowMapperService windowMapperService)
        {
            _windowMapperService = windowMapperService;
        }


        /// <summary>
        /// Настраивает закрытия окна в зависимости от того, какая у этого окна view модель
        /// </summary>
        /// <param name="window"></param>
        public void CloseWindow(Window window)
        {
            if (window.DataContext is ClientViewModel clientViewModel)
            {
                clientViewModel.ClearResources();
            }
            else if(window.DataContext is AllMessagesViewModel allMessagesViewModel)
            {
                allMessagesViewModel.ClearResources();
            }
            else
            {
                return;
                //(window.DataContext as ViewModelBase).ClearResources();
            }
            
        }


        /// <summary>
        /// Настраивает открытия окна а зависимости от того, какая у этого окна view модель
        /// </summary>
        /// <param name="viewModel"></param>
        public void ShowWindow(ViewModelBase viewModel)
        {
            var windowType = _windowMapperService.GetWindowTypeForeViewModel(viewModel.GetType());
            if (windowType != null)
            {
                var window = Activator.CreateInstance(windowType) as Window;
                window.DataContext = viewModel;
                window.Show();
                viewModel.CloseAction = new Action(window.Close);
                viewModel.ActivateAction = () => window.Activate();
                window.Closed += (sender, args) => CloseWindow(window);
            }
        }
    }
}
