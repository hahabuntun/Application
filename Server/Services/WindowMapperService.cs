using Server.ViewModels;
using Server.Views;
using System.Windows;

namespace Server.Services
{
    /// <summary>
    /// Сопоставляет view модель с окном
    /// </summary>
    public class WindowMapperService
    {
        private readonly Dictionary<Type, Type> _mappings = new Dictionary<Type, Type>();


        public WindowMapperService()
        {
            RegisterMapping<AllConnectionsViewModel, AllConnectionsView>();
            RegisterMapping<SingleConnectionViewModel, SingleConnectionView>();
            RegisterMapping<AllMessagesViewModel, AllMessagesView>();
        }


        /// <summary>
        /// Добавляет сопоставление
        /// </summary>
        /// <typeparam name="TViewModel"></typeparam>
        /// <typeparam name="TWindow"></typeparam>
        public void RegisterMapping<TViewModel, TWindow>() where TViewModel : ViewModelBase where TWindow : Window
        {
            _mappings[typeof(TViewModel)] = typeof(TWindow);
        }


        /// <summary>
        /// Получает тип окна по его view модели
        /// </summary>
        /// <param name="ViewModelType"></param>
        /// <returns></returns>
        public Type GetWindowTypeForeViewModel(Type ViewModelType)
        {
            _mappings.TryGetValue(ViewModelType, out var windowType);
            return windowType;
        }
    }
}
