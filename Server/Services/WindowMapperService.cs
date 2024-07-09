
using Server.ViewModels;
using Server.Views;
using System.Windows;

namespace Server.Services
{
    public class WindowMapperService
    {
        private readonly Dictionary<Type, Type> _mappings = new Dictionary<Type, Type>();
        public WindowMapperService()
        {
            RegisterMapping<AllConnectionsViewModel, AllConnectionsView>();
            RegisterMapping<SingleConnectionViewModel, SingleConnectionView>();
            RegisterMapping<AllMessagesViewModel, AllMessagesView>();
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
