using Server.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace Server.Services
{
    public interface IWindowManagerService
    {
        void ShowWindow(ViewModelBase viewModel);
        void CloseWindow(Window window);
    }
    public class WindowManagerService : IWindowManagerService
    {
        private readonly WindowMapperService _windowMapperService;
        public WindowManagerService(WindowMapperService windowMapperService)
        {
            _windowMapperService = windowMapperService;
        }
        public void CloseWindow(Window window)
        {
            if (window.DataContext is AllConnectionsViewModel allConnectionsViewModel)
            {
                allConnectionsViewModel.ClearResources();
            }
            else if(window.DataContext is SingleConnectionViewModel singleConnectionViewModel)
            {
                singleConnectionViewModel.ClearResources();
            }
            else
            {
                return;
                //(window.DataContext as ViewModelBase).ClearResources();
            }
            
        }

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
