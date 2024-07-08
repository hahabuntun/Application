
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Server.ViewModels
{
    public class ViewModelBase : INotifyPropertyChanged
    {
        public Action CloseAction { get; set; }
        public void CloseSelf()
        {
            CloseAction?.Invoke();
        }
        public Action ActivateAction { get; set; }
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
