using SharpMonoInjector.Gui.ViewModels;
using System.Windows;
using System.Windows.Input;

namespace SharpMonoInjector.Gui.Views
{
    public partial class SelectProfileWindow : Window
    {
        public SelectProfileWindow(SelectProfileViewModel viewModel)
        {
            InitializeComponent();
            DataContext = viewModel;
        }

        private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            DragMove();
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}