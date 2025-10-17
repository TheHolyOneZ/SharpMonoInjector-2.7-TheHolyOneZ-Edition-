using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using Microsoft.Win32;
using SharpMonoInjector.Gui.Models;
using SharpMonoInjector.Gui.Services;

namespace SharpMonoInjector.Gui.ViewModels
{
    public class LogViewerViewModel : ViewModel
    {
        private readonly LoggingService _loggingService;

        public LogViewerViewModel()
        {
            _loggingService = LoggingService.Instance;
            FilteredLogs = _loggingService.Logs;

            ClearLogsCommand = new RelayCommand(ExecuteClearLogsCommand);
            ExportLogsCommand = new RelayCommand(ExecuteExportLogsCommand);
            SearchCommand = new RelayCommand(ExecuteSearchCommand);
            FilterByLevelCommand = new RelayCommand(ExecuteFilterByLevelCommand);
            CopyLogCommand = new RelayCommand(ExecuteCopyLogCommand);
        }

        public RelayCommand ClearLogsCommand { get; }
        public RelayCommand ExportLogsCommand { get; }
        public RelayCommand SearchCommand { get; }
        public RelayCommand FilterByLevelCommand { get; }
        public RelayCommand CopyLogCommand { get; }

        private ObservableCollection<LogEntry> _filteredLogs;
        public ObservableCollection<LogEntry> FilteredLogs
        {
            get => _filteredLogs;
            set => Set(ref _filteredLogs, value);
        }

        private string _searchText;
        public string SearchText
        {
            get => _searchText;
            set
            {
                Set(ref _searchText, value);
                ExecuteSearchCommand(null);
            }
        }

        private LogLevel? _selectedLogLevel;
        public LogLevel? SelectedLogLevel
        {
            get => _selectedLogLevel;
            set
            {
                Set(ref _selectedLogLevel, value);
                ExecuteSearchCommand(null);
            }
        }

        private LogEntry _selectedLog;
        public LogEntry SelectedLog
        {
            get => _selectedLog;
            set => Set(ref _selectedLog, value);
        }

        private void ExecuteClearLogsCommand(object parameter)
        {
            var result = MessageBox.Show(
                "Are you sure you want to clear all logs?",
                "Clear Logs",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question
            );

            if (result == MessageBoxResult.Yes)
            {
                _loggingService.Clear();
            }
        }

        private void ExecuteExportLogsCommand(object parameter)
        {
            SaveFileDialog sfd = new SaveFileDialog
            {
                Filter = "Text Files|*.txt|Log Files|*.log|All Files|*.*",
                Title = "Export Logs",
                FileName = $"InjectorLogs_{DateTime.Now:yyyyMMdd_HHmmss}.txt"
            };

            if (sfd.ShowDialog() == true)
            {
                _loggingService.ExportLogs(sfd.FileName);
                MessageBox.Show($"Logs exported successfully to:\n{sfd.FileName}", "Export Complete", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void ExecuteSearchCommand(object parameter)
        {
            if (string.IsNullOrWhiteSpace(SearchText) && !SelectedLogLevel.HasValue)
            {
                FilteredLogs = _loggingService.Logs;
            }
            else
            {
                FilteredLogs = _loggingService.FilterLogs(SearchText, SelectedLogLevel);
            }
        }

        private void ExecuteFilterByLevelCommand(object parameter)
        {
            if (parameter is string levelStr)
            {
                if (levelStr == "All")
                {
                    SelectedLogLevel = null;
                }
                else if (Enum.TryParse<LogLevel>(levelStr, out var level))
                {
                    SelectedLogLevel = level;
                }
            }
        }

        private void ExecuteCopyLogCommand(object parameter)
        {
            if (SelectedLog != null)
            {
                Clipboard.SetText(SelectedLog.FullMessage);
            }
        }
    }
}