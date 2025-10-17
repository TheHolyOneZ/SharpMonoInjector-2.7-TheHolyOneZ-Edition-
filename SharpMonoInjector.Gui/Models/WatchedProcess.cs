using SharpMonoInjector.Gui.ViewModels;
using System;

namespace SharpMonoInjector.Gui.Models
{
    [Serializable]
    public class WatchedProcess : ViewModel 
    {
        private string _processName;
        public string ProcessName
        {
            get => _processName;
            set => Set(ref _processName, value);
        }

        private InjectionProfile _profile;
        public InjectionProfile Profile
        {
            get => _profile;
            set => Set(ref _profile, value);
        }

        private bool _useCurrentSettings;
        public bool UseCurrentSettings
        {
            get => _useCurrentSettings;
            set => Set(ref _useCurrentSettings, value);
        }

        public override string ToString()
        {
            if (UseCurrentSettings)
                return $"{ProcessName} → [Current Settings]";
            
            return Profile != null 
                ? $"{ProcessName} → {Profile.Name}" 
                : ProcessName;
        }
    }
}