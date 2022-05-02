using PropertyChanged;
using System.ComponentModel;

namespace PDL4.ViewModels
{
    /// <summary>
    /// A standard base class for viewmodels, implements property change event interface.
    /// </summary>
    [AddINotifyPropertyChangedInterface]
    class BaseViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged = (sender, e) => { };

        public void OnPropertyChanged(string name)
        {
            PropertyChanged(this, new PropertyChangedEventArgs(name));
        }
    }
}
