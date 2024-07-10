using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Server.ViewModels
{
    public class ViewModelBase : INotifyPropertyChanged
    {
        public Action CloseAction { get; set; } //event при закрытии окна
        //закрытие себя
        public void CloseSelf()
        {
            CloseAction?.Invoke();
        }
        public Action ActivateAction { get; set; } //event при активации окна
        public virtual void ClearResources(){ } //очистка ресурсов окна
        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
