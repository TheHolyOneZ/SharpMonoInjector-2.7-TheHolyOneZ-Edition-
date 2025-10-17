using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace SharpMonoInjector.Gui
{
    public partial class App : Application
    {
        public App()
        {
            try
            {
                string logPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "DebugLog.txt");
                if (File.Exists(logPath))
                {
                    File.Delete(logPath);
                }
            }
            catch { }

            this.DispatcherUnhandledException += (s, e) =>
            {
                try
                {
                    string errorLog = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ErrorLog.txt");
                    File.AppendAllText(errorLog, $"[{DateTime.Now}] {e.Exception.Message}\n{e.Exception.StackTrace}\n\n");
                }
                catch { }
                
                MessageBox.Show($"Application Error: {e.Exception.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                e.Handled = true;
            };

            this.Exit += OnApplicationExit;
        }

        private void OnApplicationExit(object sender, ExitEventArgs e)
        {
            
        }
    }
}