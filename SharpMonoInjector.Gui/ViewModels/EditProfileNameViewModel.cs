using System.Windows;

namespace SharpMonoInjector.Gui.ViewModels
{
    public class EditProfileNameViewModel : ViewModel
    {
        private string _currentName;
        public string CurrentName
        {
            get => _currentName;
            set => Set(ref _currentName, value);
        }

        private string _newName;
        public string NewName
        {
            get => _newName;
            set
            {
                Set(ref _newName, value);
                ValidateName();
            }
        }

        private string _validationMessage;
        public string ValidationMessage
        {
            get => _validationMessage;
            set => Set(ref _validationMessage, value);
        }

        private bool _hasValidationError;
        public bool HasValidationError
        {
            get => _hasValidationError;
            set => Set(ref _hasValidationError, value);
        }

        public RelayCommand SaveCommand { get; }

        public EditProfileNameViewModel(string currentName)
        {
            CurrentName = currentName;
            NewName = currentName; // Pre-fill with current name
            SaveCommand = new RelayCommand(ExecuteSave, CanExecuteSave);
            ValidateName();
        }

        private void ValidateName()
        {
            if (string.IsNullOrWhiteSpace(NewName))
            {
                ValidationMessage = "Profile name cannot be empty";
                HasValidationError = true;
            }
            else if (NewName.Length > 100)
            {
                ValidationMessage = "Profile name cannot exceed 100 characters";
                HasValidationError = true;
            }
            else if (NewName.Trim() != NewName)
            {
                ValidationMessage = "Profile name cannot start or end with spaces";
                HasValidationError = true;
            }
            else
            {
                ValidationMessage = string.Empty;
                HasValidationError = false;
            }
        }

        private bool CanExecuteSave(object parameter)
        {
            return !string.IsNullOrWhiteSpace(NewName) &&
                   NewName.Length <= 100 &&
                   NewName.Trim() == NewName;
        }

        private void ExecuteSave(object parameter)
        {
            if (parameter is Window window)
            {
                // Store the result in a way the parent can access it
                window.Tag = NewName.Trim();
                window.DialogResult = true;
                window.Close();
            }
        }
    }
}