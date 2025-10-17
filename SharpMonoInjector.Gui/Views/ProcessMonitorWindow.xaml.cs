using SharpMonoInjector.Gui.ViewModels;
using System.Windows;
using System.Windows.Input;

namespace SharpMonoInjector.Gui.Views
{
    public partial class ProcessMonitorWindow : Window
    {
        public ProcessMonitorWindow(MainWindowViewModel viewModel)
        {
            InitializeComponent();
            DataContext = viewModel; 
        }

        private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            try { DragMove(); } catch { }
        }

        private void Window_Exit(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}