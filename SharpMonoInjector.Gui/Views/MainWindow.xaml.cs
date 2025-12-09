using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Security.Principal;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using SharpMonoInjector.Gui.Models;
using SharpMonoInjector.Gui.Services;
using SharpMonoInjector.Gui.ViewModels;

namespace SharpMonoInjector.Gui.Views
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            bool IsElevated = new WindowsPrincipal(WindowsIdentity.GetCurrent()).IsInRole(WindowsBuiltInRole.Administrator);

#if RELEASE
            if (!IsElevated)
            {
                try
                {
                    string exeName = Process.GetCurrentProcess().MainModule.FileName;
                    ProcessStartInfo startInfo = new ProcessStartInfo(exeName);
                    startInfo.Verb = "runas";
                    startInfo.UseShellExecute = true;
                    Process.Start(startInfo);
                    Environment.Exit(0);
                    return;
                }
                catch
                {
                    MessageBox.Show("This application requires administrator privileges to run.", "Elevation Required", MessageBoxButton.OK, MessageBoxImage.Warning);
                    Environment.Exit(1);
                    return;
                }
            }
#endif
            InitializeComponent();
        }

        private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            try
            {
                DragMove();
            }
            catch { }
        }

        private void Window_Exit(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }

        private void Window_Minimize(object sender, RoutedEventArgs e)
        {
            Application.Current.MainWindow.WindowState = WindowState.Minimized;
        }

        private void Window_Maximize(object sender, RoutedEventArgs e)
        {
            if (Application.Current.MainWindow.WindowState == WindowState.Maximized)
            {
                Application.Current.MainWindow.WindowState = WindowState.Normal;
            }
            else
            {
                Application.Current.MainWindow.WindowState = WindowState.Maximized;
            }
        }

        private void Window_Closing(object sender, CancelEventArgs e)
        {
            try
            {
                if (DataContext is MainWindowViewModel viewModel)
                {
                    viewModel.Cleanup();
                }
            }
            catch { }
        }

        private void DesignerLink_Click(object sender, MouseButtonEventArgs e)
        {
            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = "https://github.com/TheHolyOneZ",
                    UseShellExecute = true
                });
            }
            catch { }
        }

        private void RecentAssembly_DoubleClick(object sender, MouseButtonEventArgs e)
        {
            try
            {
                var listBox = sender as ListBox;
                if (listBox?.SelectedItem is string assemblyPath)
                {
                    var vm = DataContext as MainWindowViewModel;
                    vm?.SelectRecentAssemblyCommand.Execute(assemblyPath);
                }
            }
            catch { }
        }

        private void RecentProfile_DoubleClick(object sender, MouseButtonEventArgs e)
        {
            try
            {
                var listBox = sender as ListBox;
                if (listBox?.SelectedItem is InjectionProfile profile)
                {
                    var vm = DataContext as MainWindowViewModel;
                    vm?.SelectRecentProfileCommand.Execute(profile);
                }
            }
            catch { }
        }

        private void RecentAssembly_Click(object sender, MouseButtonEventArgs e)
        {
            try
            {
                if (e.ClickCount == 1)
                {
                    var textBlock = sender as TextBlock;
                    if (textBlock?.DataContext is string assemblyPath)
                    {
                        var vm = DataContext as MainWindowViewModel;
                        vm?.SelectRecentAssemblyCommand.Execute(assemblyPath);
                    }
                }
            }
            catch { }
        }

        private void RecentProfile_Click(object sender, MouseButtonEventArgs e)
        {
            try
            {
                if (e.ClickCount == 1)
                {
                    var stackPanel = sender as StackPanel;
                    if (stackPanel?.DataContext is InjectionProfile profile)
                    {
                        var vm = DataContext as MainWindowViewModel;
                        vm?.SelectRecentProfileCommand.Execute(profile);
                    }
                }
            }
            catch { }
        }

        private void OpenLogViewer_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var logWindow = new LogViewerWindow();
                logWindow.Show();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to open log viewer: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ShowKeyboardShortcuts_Click(object sender, RoutedEventArgs e)
        {
            string shortcuts = "SharpMonoInjector Keyboard Shortcuts:\n\n" +
                "Global Shortcuts:\n" +
                "• Ctrl+O - Browse for assembly file\n" +
                "• Ctrl+I - Open assembly inspector\n" +
                "• Ctrl+L - Open log viewer\n" +
                "• F5 - Inject assembly\n" +
                "• F6 - Eject assembly\n" +
                "• F9 - Refresh process list\n\n" +
                "Injection Panel:\n" +
                "• Use Browse button to select assembly\n" +
                "• Use Inspect button to auto-populate fields\n\n" +
                "Ejection Panel:\n" +
                "• Select an injected assembly to eject\n" +
                "• Use Reload button for hot-swapping [EXPERIMENTAL]\n" +
                "• Use Force Remove for cleanup\n\n" +
                "Process Monitor:\n" +
                "• Add processes to watch for auto-injection\n" +
                "• Configure per-process profiles\n" +
                "• Enable/disable monitoring as needed\n\n" +
                "Tip: Hover over buttons for tooltips!";

            MessageBox.Show(shortcuts, "Keyboard Shortcuts & Usage Guide",
                MessageBoxButton.OK, MessageBoxImage.Information);
        }



    }
}