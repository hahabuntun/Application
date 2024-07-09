
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Client.ViewModels
{
    /// <summary>
    /// Базовый класс view модели
    /// </summary>
    public class ViewModelBase : INotifyPropertyChanged
    {
        /// <summary>
        /// События, срабатывающие при активации окна и его открытии
        /// </summary>
        public Action CloseAction { get; set; }
        public Action ActivateAction { get; set; }


        /// <summary>
        /// Функция закрытия окна
        /// Я забыл ее использовать
        /// </summary>
        public void CloseSelf()
        {
            CloseAction?.Invoke();
        }
        

        /// <summary>
        /// Функция очистки ресурсов
        /// </summary>
        public virtual void ClearResources()
        {

        }


        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
