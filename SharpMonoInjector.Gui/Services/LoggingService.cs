using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Windows;
using SharpMonoInjector.Gui.Models;

namespace SharpMonoInjector.Gui.Services
{
    public class LoggingService
    {
        private static LoggingService _instance;
        private static readonly object _lock = new object();

        public ObservableCollection<LogEntry> Logs { get; private set; }

        public static LoggingService Instance
        {
            get
            {
                if (_instance == null)
                {
                    lock (_lock)
                    {
                        if (_instance == null)
                            _instance = new LoggingService();
                    }
                }
                return _instance;
            }
        }

        private LoggingService()
        {
            Logs = new ObservableCollection<LogEntry>();
            Log(LogLevel.Info, "Logging service initialized", "System");
        }

        public void Log(LogLevel level, string message, string source = "")
        {
            Application.Current?.Dispatcher.Invoke(() =>
            {
                var entry = new LogEntry(level, message, source);
                Logs.Add(entry);

                File.AppendAllText(
                    Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "DebugLog.txt"),
                    entry.FullMessage + "\r\n"
                );
            });
        }

        public void Info(string message, string source = "") => Log(LogLevel.Info, message, source);
        public void Warning(string message, string source = "") => Log(LogLevel.Warning, message, source);
        public void Error(string message, string source = "") => Log(LogLevel.Error, message, source);
        public void Success(string message, string source = "") => Log(LogLevel.Success, message, source);

        public void Clear()
        {
            Application.Current?.Dispatcher.Invoke(() =>
            {
                Logs.Clear();
                Log(LogLevel.Info, "Logs cleared", "System");
            });
        }

        public void ExportLogs(string filePath)
        {
            try
            {
                var logText = string.Join("\r\n", Logs.Select(l => l.FullMessage));
                File.WriteAllText(filePath, logText);
                Log(LogLevel.Success, $"Logs exported to {filePath}", "System");
            }
            catch (Exception ex)
            {
                Log(LogLevel.Error, $"Failed to export logs: {ex.Message}", "System");
            }
        }

        public ObservableCollection<LogEntry> FilterLogs(string searchText, LogLevel? levelFilter = null)
        {
            var filtered = Logs.AsEnumerable();

            if (!string.IsNullOrEmpty(searchText))
                filtered = filtered.Where(l => l.Message.IndexOf(searchText, StringComparison.OrdinalIgnoreCase) >= 0);

            if (levelFilter.HasValue)
                filtered = filtered.Where(l => l.Level == levelFilter.Value);

            return new ObservableCollection<LogEntry>(filtered);
        }
    }
}