using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using SharpMonoInjector.Gui.ViewModels;

namespace SharpMonoInjector.Gui.Views
{
    public partial class LogViewerWindow : Window
    {
        public LogViewerWindow()
        {
            InitializeComponent();
        }

        private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
                DragMove();
        }

        private void Window_Minimize(object sender, RoutedEventArgs e)
        {
            WindowState = WindowState.Minimized;
        }

        private void Window_Close(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void FilterComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var comboBox = sender as ComboBox;
            if (comboBox?.SelectedItem is ComboBoxItem item)
            {
                var vm = DataContext as LogViewerViewModel;
                vm?.FilterByLevelCommand.Execute(item.Content.ToString().Replace(" Levels", ""));
            }
        }
    }
}