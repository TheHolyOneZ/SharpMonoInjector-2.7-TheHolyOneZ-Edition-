using SharpMonoInjector.Gui.ViewModels;
using System.Windows;
using System.Windows.Input;

namespace SharpMonoInjector.Gui.Views
{
    public partial class AssemblyInspectorWindow : Window
    {
        public AssemblyInspectorViewModel ViewModel { get; }

        public AssemblyInspectorWindow()
        {
            InitializeComponent();
            ViewModel = new AssemblyInspectorViewModel();
            DataContext = ViewModel;
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
    }
}