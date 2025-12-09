using SharpMonoInjector.Gui.ViewModels;
using System;
using System.Windows;
using System.Windows.Input;

namespace SharpMonoInjector.Gui.Views
{
    public partial class EditProfileNameWindow : Window
    {
        public EditProfileNameViewModel ViewModel { get; }

        public EditProfileNameWindow(string currentName)
        {
            InitializeComponent();
            ViewModel = new EditProfileNameViewModel(currentName);
            DataContext = ViewModel;

            // Focus and select all text in the text box when window loads
            Loaded += (s, e) =>
            {
                Dispatcher.BeginInvoke(new Action(() =>
                {
                    NewNameTextBox.Focus();
                    NewNameTextBox.SelectAll();
                }), System.Windows.Threading.DispatcherPriority.Input);
            };
        }

        private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ButtonState == MouseButtonState.Pressed)
            {
                DragMove();
            }
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}