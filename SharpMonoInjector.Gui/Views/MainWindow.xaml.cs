using System;
using System.Diagnostics;
using System.Security.Principal;
using System.Windows;
using System.Windows.Input;

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
                string exeName = System.Diagnostics.Process.GetCurrentProcess().MainModule.FileName;
                ProcessStartInfo startInfo = new ProcessStartInfo(exeName);
                startInfo.Verb = "runas";
                System.Diagnostics.Process.Start(startInfo);
                AppDomain.Unload(AppDomain.CurrentDomain);
            }
#endif
            InitializeComponent();
        }

        #region[Window Events]

        private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            DragMove();
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

        private void DesignerLink_Click(object sender, MouseButtonEventArgs e)
        {
            try
            {
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                {
                    FileName = "https://github.com/TheHolyOneZ",
                    UseShellExecute = true
                });
            }
            catch { }
        }

        #endregion
    }
}