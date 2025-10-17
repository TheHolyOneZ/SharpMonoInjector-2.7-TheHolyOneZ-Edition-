using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using SharpMonoInjector.Gui.Models;

namespace SharpMonoInjector.Gui.Services
{
    public class ProcessDetectedEventArgs : EventArgs
    {
        public MonoProcess Process { get; set; }
        public WatchedProcess WatchedProcessInfo { get; set; }
    }

    public class ProcessMonitorService : IDisposable
    {
        private CancellationTokenSource _cancellationTokenSource;
        private Task _monitorTask;
        private readonly HashSet<int> _detectedProcessIds = new HashSet<int>();
        private ProcessMonitorSettings _settings;
        private readonly LoggingService _loggingService;
        private bool _isDisposed = false;

        public event EventHandler<ProcessDetectedEventArgs> ProcessDetected;

        public bool IsMonitoring => _monitorTask != null && !_monitorTask.IsCompleted;

        public ProcessMonitorService(LoggingService loggingService)
        {
            _loggingService = loggingService;
        }

        public void Start(ProcessMonitorSettings settings)
        {
            if (IsMonitoring || _isDisposed)
                return;

            try
            {
                _settings = settings ?? throw new ArgumentNullException(nameof(settings));
                _cancellationTokenSource = new CancellationTokenSource();
                _detectedProcessIds.Clear();

                var existingProcesses = GetMonoProcesses()
                    .Where(p => _settings.WatchedProcesses.Any(w => 
                        p.Name.Equals(w.ProcessName, StringComparison.OrdinalIgnoreCase)))
                    .ToList();

                foreach (var proc in existingProcesses)
                {
                    _detectedProcessIds.Add(proc.Id);
                }

                _monitorTask = Task.Run(() => MonitorLoop(_cancellationTokenSource.Token), _cancellationTokenSource.Token);
                _loggingService?.Log(LogLevel.Info, $"Process monitor started. Watching: {string.Join(", ", _settings.WatchedProcesses.Select(w => w.ProcessName))}");
            }
            catch (Exception ex)
            {
                _loggingService?.Log(LogLevel.Error, $"Failed to start monitor: {ex.Message}");
                Stop();
            }
        }

        public void Stop()
        {
            if (!IsMonitoring)
                return;

            try
            {
                _cancellationTokenSource?.Cancel();
                
                if (_monitorTask != null && !_monitorTask.Wait(5000))
                {
                    _loggingService?.Log(LogLevel.Warning, "Monitor task did not complete in time");
                }
                
                _cancellationTokenSource?.Dispose();
                _cancellationTokenSource = null;
                _monitorTask = null;
                _detectedProcessIds.Clear();

                _loggingService?.Log(LogLevel.Info, "Process monitor stopped");
            }
            catch (Exception ex)
            {
                _loggingService?.Log(LogLevel.Error, $"Error stopping monitor: {ex.Message}");
            }
        }

        private async Task MonitorLoop(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                try
                {
                    var monoProcesses = GetMonoProcesses();

                    foreach (var process in monoProcesses)
                    {
                        if (token.IsCancellationRequested)
                            break;

                        var watchedProcess = _settings.WatchedProcesses.FirstOrDefault(w => 
                            process.Name.Equals(w.ProcessName, StringComparison.OrdinalIgnoreCase));

                        if (watchedProcess != null)
                        {
                            if (!_detectedProcessIds.Contains(process.Id))
                            {
                                _detectedProcessIds.Add(process.Id);
                                OnProcessDetected(process, watchedProcess);
                            }
                        }
                    }

                    _detectedProcessIds.RemoveWhere(pid => 
                    {
                        try
                        {
                            var proc = Process.GetProcessById(pid);
                            return proc.HasExited;
                        }
                        catch
                        {
                            return true;
                        }
                    });

                    await Task.Delay(_settings.MonitorIntervalMs, token);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    _loggingService?.Log(LogLevel.Error, $"Monitor error: {ex.Message}");
                    await Task.Delay(1000, token);
                }
            }
        }

        private List<MonoProcess> GetMonoProcesses()
        {
            var result = new List<MonoProcess>();
            
            try
            {
                var processes = Process.GetProcesses();

                foreach (var process in processes)
                {
                    IntPtr handle = IntPtr.Zero;
                    try
                    {
                        handle = Native.OpenProcess(
                            ProcessAccessRights.PROCESS_QUERY_INFORMATION | ProcessAccessRights.PROCESS_VM_READ, 
                            false, 
                            process.Id);
                        
                        if (handle == IntPtr.Zero)
                            continue;

                        if (ProcessUtils.GetMonoModule(handle, out IntPtr monoModule))
                        {
                            result.Add(new MonoProcess
                            {
                                Id = process.Id,
                                Name = process.ProcessName,
                                MonoModule = monoModule
                            });
                        }
                    }
                    catch { }
                    finally
                    {
                        if (handle != IntPtr.Zero)
                            Native.CloseHandle(handle);
                    }
                }
            }
            catch (Exception ex)
            {
                _loggingService?.Log(LogLevel.Error, $"Error scanning processes: {ex.Message}");
            }

            return result;
        }

        private void OnProcessDetected(MonoProcess process, WatchedProcess watchedProcessInfo)
        {
            try
            {
                Application.Current?.Dispatcher.BeginInvoke(new Action(() =>
                {
                    try
                    {
                        _loggingService?.Log(LogLevel.Success, $"Detected process: {process.Name} (PID: {process.Id})");
                        
                        if (_settings.ShowNotifications)
                        {
                            ShowNotification(process, watchedProcessInfo);
                        }

                        ProcessDetected?.Invoke(this, new ProcessDetectedEventArgs 
                        { 
                            Process = process,
                            WatchedProcessInfo = watchedProcessInfo
                        });
                    }
                    catch (Exception ex)
                    {
                        _loggingService?.Log(LogLevel.Error, $"Error in process detected handler: {ex.Message}");
                    }
                }));
            }
            catch (Exception ex)
            {
                _loggingService?.Log(LogLevel.Error, $"Error invoking process detected: {ex.Message}");
            }
        }

        private void ShowNotification(MonoProcess process, WatchedProcess watchedProcessInfo)
        {
            try
            {
                var notificationWindow = new Window
                {
                    WindowStyle = WindowStyle.None,
                    AllowsTransparency = true,
                    Background = System.Windows.Media.Brushes.Transparent,
                    ShowInTaskbar = false,
                    Topmost = true,
                    Width = 350,
                    Height = 120,
                    Left = SystemParameters.WorkArea.Right - 370,
                    Top = SystemParameters.WorkArea.Bottom - 140
                };

                var border = new System.Windows.Controls.Border
                {
                    Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(30, 30, 30)),
                    BorderBrush = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(0, 230, 118)),
                    BorderThickness = new Thickness(2),
                    CornerRadius = new System.Windows.CornerRadius(8),
                    Padding = new Thickness(15)
                };

                var stackPanel = new System.Windows.Controls.StackPanel();

                var titleBlock = new System.Windows.Controls.TextBlock
                {
                    Text = "🎯 Process Detected",
                    Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(0, 230, 118)),
                    FontWeight = FontWeights.Bold,
                    FontSize = 14,
                    Margin = new Thickness(0, 0, 0, 8)
                };

                var messageBlock = new System.Windows.Controls.TextBlock
                {
                    Text = $"{process.Name} (PID: {process.Id})",
                    Foreground = System.Windows.Media.Brushes.White,
                    FontSize = 12,
                    Margin = new Thickness(0, 0, 0, 4)
                };

                string profileText = watchedProcessInfo.UseCurrentSettings 
                    ? "Using current settings" 
                    : watchedProcessInfo.Profile != null 
                        ? $"Profile: {watchedProcessInfo.Profile.Name}" 
                        : "No profile set";

                var profileBlock = new System.Windows.Controls.TextBlock
                {
                    Text = profileText,
                    Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(176, 176, 176)),
                    FontSize = 10,
                    Margin = new Thickness(0, 0, 0, 4)
                };

                var statusBlock = new System.Windows.Controls.TextBlock
                {
                    Text = _settings.AutoInjectOnDetection ? "Auto-injecting..." : "Ready for injection",
                    Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(176, 176, 176)),
                    FontSize = 11,
                    FontStyle = FontStyles.Italic
                };

                stackPanel.Children.Add(titleBlock);
                stackPanel.Children.Add(messageBlock);
                stackPanel.Children.Add(profileBlock);
                stackPanel.Children.Add(statusBlock);
                border.Child = stackPanel;
                notificationWindow.Content = border;

                notificationWindow.Show();

                var timer = new System.Windows.Threading.DispatcherTimer
                {
                    Interval = TimeSpan.FromSeconds(4)
                };
                timer.Tick += (s, e) =>
                {
                    timer.Stop();
                    notificationWindow.Close();
                };
                timer.Start();
            }
            catch (Exception ex)
            {
                _loggingService?.Log(LogLevel.Error, $"Failed to show notification: {ex.Message}");
            }
        }

        public void Dispose()
        {
            if (!_isDisposed)
            {
                Stop();
                _isDisposed = true;
            }
        }
    }
}