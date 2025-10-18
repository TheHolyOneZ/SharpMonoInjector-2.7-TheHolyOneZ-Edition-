using SharpMonoInjector.Gui.ViewModels;

namespace SharpMonoInjector.Gui.ViewModels
{
    public class ChooseBepInExViewModel : ViewModel
    {
        private BepInExType _selectedType;
        private string _gameName;
        private string _standardPath;
        private string _modManagerPath;

        public ChooseBepInExViewModel(string gameName, string standardPath, string modManagerPath)
        {
            _gameName = gameName;
            _standardPath = standardPath;
            _modManagerPath = modManagerPath;
            _selectedType = BepInExType.Standard;
        }

        public BepInExType SelectedType
        {
            get => _selectedType;
            set => Set(ref _selectedType, value);
        }

        public string GameName
        {
            get => _gameName;
            set => Set(ref _gameName, value);
        }

        public string StandardPath
        {
            get => _standardPath;
            set => Set(ref _standardPath, value);
        }

        public string ModManagerPath
        {
            get => _modManagerPath;
            set => Set(ref _modManagerPath, value);
        }

        public bool IsStandardSelected
        {
            get => _selectedType == BepInExType.Standard;
            set
            {
                if (value) SelectedType = BepInExType.Standard;
            }
        }

        public bool IsModManagerSelected
        {
            get => _selectedType == BepInExType.ModManager;
            set
            {
                if (value) SelectedType = BepInExType.ModManager;
            }
        }
    }
}