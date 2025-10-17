using SharpMonoInjector.Gui.Models;
using System.Collections.ObjectModel;
using System.Windows;

namespace SharpMonoInjector.Gui.ViewModels
{
    public class SelectProfileViewModel : ViewModel
    {
       
        private string _title;
        public string Title
        {
            get => _title;
            set => Set(ref _title, value);
        }

        public ObservableCollection<InjectionProfile> AvailableProfiles { get; }
        public WatchedProcess Result { get; private set; }

        private bool _useCurrentSettings = true;
        public bool UseCurrentSettings
        {
            get => _useCurrentSettings;
            set => Set(ref _useCurrentSettings, value);
        }

        private InjectionProfile _selectedProfile;
        public InjectionProfile SelectedProfile
        {
            get => _selectedProfile;
            set => Set(ref _selectedProfile, value);
        }

        public RelayCommand SelectCommand { get; }
        public RelayCommand CancelCommand { get; }

        public SelectProfileViewModel(ObservableCollection<InjectionProfile> profiles, WatchedProcess currentProcess)
        {
            AvailableProfiles = profiles;
            UseCurrentSettings = currentProcess.UseCurrentSettings;
            SelectedProfile = currentProcess.Profile;

            SelectCommand = new RelayCommand(p => ExecuteSelect(p as Window, currentProcess));
            CancelCommand = new RelayCommand(p => ExecuteCancel(p as Window));
        }

        private void ExecuteSelect(Window window, WatchedProcess originalProcess)
        {
            if (window == null) return;

            // When creating the result use the original process name
            Result = new WatchedProcess
            {
                ProcessName = originalProcess.ProcessName,
                UseCurrentSettings = this.UseCurrentSettings,
                Profile = this.UseCurrentSettings ? null : this.SelectedProfile
            };
            window.DialogResult = true;
            window.Close();
        }

        private void ExecuteCancel(Window window)
        {
            if (window == null) return;
            window.DialogResult = false;
            window.Close();
        }
    }
}