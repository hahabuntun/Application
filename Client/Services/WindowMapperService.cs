
using Client.ViewModels;
using Client.Views;
using System.Windows;

namespace Client.Services
{
    public class WindowMapperService
    {
        private readonly Dictionary<Type, Type> _mappings = new Dictionary<Type, Type>();
        public WindowMapperService()
        {
            RegisterMapping<ClientViewModel, ClientView>();
        }
        public void RegisterMapping<TViewModel, TWindow>() where TViewModel : ViewModelBase where TWindow : Window
        {
            _mappings[typeof(TViewModel)] = typeof(TWindow);
        }
        public Type GetWindowTypeForeViewModel(Type ViewModelType)
        {
            _mappings.TryGetValue(ViewModelType, out var windowType);
            return windowType;
        }
    }
}
