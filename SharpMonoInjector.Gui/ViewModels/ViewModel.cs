using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace SharpMonoInjector.Gui.ViewModels
{
    public abstract class ViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        protected bool Set<T>(ref T property, T value, [CallerMemberName]string name = null)
        {
            if (!EqualityComparer<T>.Default.Equals(property, value))
            {
                property = value;
                RaisePropertyChanged(name);
                return true; // Return true if changed
            }
            return false; // Return false if not changed
        }

        protected void RaisePropertyChanged(string name)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}