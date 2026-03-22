using System;
using System.ComponentModel;

namespace SharpMonoInjector.Gui.Models
{
    public class MonoProcess : INotifyPropertyChanged
    {
        public IntPtr MonoModule { get; set; }
        public string Name { get; set; }
        public int Id { get; set; }

        private bool _isAlive = true;
        public bool IsAlive
        {
            get => _isAlive;
            set
            {
                if (_isAlive != value)
                {
                    _isAlive = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsAlive)));
                }
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
    }
}
