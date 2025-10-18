using System.Windows;
using System.Windows.Input;
using SharpMonoInjector.Gui.ViewModels;

namespace SharpMonoInjector.Gui.Views
{
    public partial class ChooseBepInExWindow : Window
    {
        public ChooseBepInExWindow(ChooseBepInExViewModel viewModel)
        {
            InitializeComponent();
            DataContext = viewModel;
        }

        private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            try
            {
                DragMove();
            }
            catch { }
        }

        private void Confirm_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
            Close();
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}