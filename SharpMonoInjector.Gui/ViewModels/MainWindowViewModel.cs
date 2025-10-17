using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Security.Principal;
using System.Threading.Tasks;
using System.Windows;
using System.Management;
using Microsoft.Win32;
using SharpMonoInjector.Gui.Models;
using SharpMonoInjector.Gui.Services;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualBasic;
using SharpMonoInjector.Gui.Views;


namespace SharpMonoInjector.Gui.ViewModels
{
    public class MainWindowViewModel : ViewModel
    {
        private readonly ConfigurationService _configService;
        private readonly LoggingService _loggingService;
        private readonly ProcessMonitorService _monitorService;
        private AppSettings _settings;

        public MainWindowViewModel()
        {
            _configService = ConfigurationService.Instance;
            _loggingService = LoggingService.Instance;
            _monitorService = new ProcessMonitorService(_loggingService);
            _settings = _configService.LoadSettings();

            Processes = new ObservableCollection<MonoProcess>();

            _loggingService.Info("SharpMonoInjector started", "System");

            AVAlert = AntivirusInstalled();
            if (AVAlert) { AVColor = "#FFA00668"; } else { AVColor = "#FF21AC40"; }

            RefreshCommand = new RelayCommand(ExecuteRefreshCommand, CanExecuteRefreshCommand);
            BrowseCommand = new RelayCommand(ExecuteBrowseCommand);
            InjectCommand = new RelayCommand(ExecuteInjectCommand, CanExecuteInjectCommand);
            EjectCommand = new RelayCommand(ExecuteEjectCommand, CanExecuteEjectCommand);
            CopyStatusCommand = new RelayCommand(ExecuteCopyStatusCommand);
            SaveProfileCommand = new RelayCommand(ExecuteSaveProfileCommand, CanExecuteSaveProfileCommand);
            LoadProfileCommand = new RelayCommand(ExecuteLoadProfileCommand);
            ClearRecentsCommand = new RelayCommand(ExecuteClearRecentsCommand);
            SelectRecentAssemblyCommand = new RelayCommand(ExecuteSelectRecentAssemblyCommand);
            SelectRecentProfileCommand = new RelayCommand(ExecuteSelectRecentProfileCommand);
            DeleteRecentAssemblyCommand = new RelayCommand(ExecuteDeleteRecentAssemblyCommand);
            DeleteRecentProfileCommand = new RelayCommand(ExecuteDeleteRecentProfileCommand);
            EditProfileNameCommand = new RelayCommand(ExecuteEditProfileNameCommand);
            ToggleMonitorCommand = new RelayCommand(ExecuteToggleMonitorCommand);
            AddWatchProcessCommand = new RelayCommand(ExecuteAddWatchProcessCommand, CanExecuteAddWatchProcessCommand);
            RemoveWatchProcessCommand = new RelayCommand(ExecuteRemoveWatchProcessCommand);
            SetWatchProfileCommand = new RelayCommand(ExecuteSetWatchProfileCommand);
            OpenProcessMonitorCommand = new RelayCommand(ExecuteOpenProcessMonitorCommand);

            _monitorService.ProcessDetected += OnProcessDetected;

            LoadSettings();
        }

        #region[Commands]

        public RelayCommand RefreshCommand { get; }
        public RelayCommand BrowseCommand { get; }
        public RelayCommand InjectCommand { get; }
        public RelayCommand EjectCommand { get; }
        public RelayCommand CopyStatusCommand { get; }
        public RelayCommand SaveProfileCommand { get; }
        public RelayCommand LoadProfileCommand { get; }
        public RelayCommand ClearRecentsCommand { get; }
        public RelayCommand SelectRecentAssemblyCommand { get; }
        public RelayCommand SelectRecentProfileCommand { get; }
        public RelayCommand DeleteRecentAssemblyCommand { get; }
        public RelayCommand DeleteRecentProfileCommand { get; }
        public RelayCommand EditProfileNameCommand { get; }
        public RelayCommand ToggleMonitorCommand { get; }
        public RelayCommand AddWatchProcessCommand { get; }
        public RelayCommand RemoveWatchProcessCommand { get; }
        public RelayCommand SetWatchProfileCommand { get; }
        public RelayCommand OpenProcessMonitorCommand { get; }

        private void ExecuteOpenProcessMonitorCommand(object parameter)
        {
            try
            {
                var monitorWindow = new ProcessMonitorWindow(this);
                monitorWindow.Show();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to open process monitor: {ex.Message}");
            }
        }

        private void LoadSettings()
        {
            UseStealthMode = _settings.StealthModeDefault;
            
            RecentAssemblies = new ObservableCollection<string>(_settings.RecentAssemblies);
            RecentProfiles = new ObservableCollection<InjectionProfile>(_settings.RecentProfiles);
            WatchedProcesses = new ObservableCollection<WatchedProcess>(_settings.MonitorSettings.WatchedProcesses);

            AutoInject = _settings.MonitorSettings.AutoInjectOnDetection;
            ShowNotifications = _settings.MonitorSettings.ShowNotifications;

            if (_settings.AutoLoadLastProfile && _settings.LastProfile != null)
            {
                LoadProfile(_settings.LastProfile);
                Status = "Last profile loaded";
            }

            if (_settings.MonitorSettings.IsEnabled && WatchedProcesses.Count > 0)
            {
                ExecuteToggleMonitorCommand(null);
            }
        }

        private void SaveCurrentSettings()
        {
            _settings.LastAssemblyPath = AssemblyPath;
            _settings.StealthModeDefault = UseStealthMode;
            _settings.MonitorSettings.WatchedProcesses = WatchedProcesses.ToList();
            _settings.MonitorSettings.AutoInjectOnDetection = AutoInject;
            _settings.MonitorSettings.ShowNotifications = ShowNotifications;
            _settings.MonitorSettings.IsEnabled = IsMonitoring;
            _configService.SaveSettings(_settings);
        }

        private void LoadProfile(InjectionProfile profile)
        {
            if (profile == null) return;

            AssemblyPath = profile.AssemblyPath;
            InjectNamespace = profile.Namespace;
            InjectClassName = profile.ClassName;
            InjectMethodName = profile.MethodName;
            EjectNamespace = profile.EjectNamespace;
            EjectClassName = profile.EjectClassName;
            EjectMethodName = profile.EjectMethodName;
            UseStealthMode = profile.UseStealthMode;
        }

        private bool CanExecuteSaveProfileCommand(object parameter)
        {
            return !string.IsNullOrEmpty(AssemblyPath) &&
                   !string.IsNullOrEmpty(InjectClassName) &&
                   !string.IsNullOrEmpty(InjectMethodName);
        }

        private void ExecuteSaveProfileCommand(object parameter)
        {
            var profile = new InjectionProfile(
                AssemblyPath,
                InjectNamespace,
                InjectClassName,
                InjectMethodName,
                UseStealthMode
            );

            _settings.AddRecentProfile(profile);
            _settings.LastProfile = profile;
            _configService.SaveSettings(_settings);

            RecentProfiles = new ObservableCollection<InjectionProfile>(_settings.RecentProfiles);
            Status = $"Profile '{profile.Name}' saved";
        }

        private void ExecuteLoadProfileCommand(object parameter)
        {
            if (parameter is InjectionProfile profile)
            {
                LoadProfile(profile);
                Status = $"Profile '{profile.Name}' loaded";
            }
        }

        private void ExecuteClearRecentsCommand(object parameter)
        {
            _configService.ClearAllRecents();
            _settings = _configService.LoadSettings();
            RecentAssemblies.Clear();
            RecentProfiles.Clear();
            Status = "Recent items cleared";
        }

        private void ExecuteSelectRecentAssemblyCommand(object parameter)
        {
            if (parameter is string path)
            {
                AssemblyPath = path;
            }
        }

        private void ExecuteSelectRecentProfileCommand(object parameter)
        {
            ExecuteLoadProfileCommand(parameter);
        }

        private void ExecuteDeleteRecentAssemblyCommand(object parameter)
        {
            if (parameter is string path)
            {
                _settings.RecentAssemblies.Remove(path);
                RecentAssemblies.Remove(path);
                _configService.SaveSettings(_settings);
                Status = "Assembly removed from recents";
            }
        }

        private void ExecuteDeleteRecentProfileCommand(object parameter)
        {
            if (parameter is InjectionProfile profile)
            {
                _settings.RecentProfiles.Remove(profile);
                RecentProfiles.Remove(profile);
                _configService.SaveSettings(_settings);
                Status = "Profile removed from recents";
                _loggingService.Info($"Profile '{profile.Name}' removed", "ProfileManager");
            }
        }

        private void ExecuteEditProfileNameCommand(object parameter)
        {
            if (parameter is InjectionProfile profile)
            {
                string newName = Interaction.InputBox(
                    "Enter new profile name:",
                    "Edit Profile Name",
                    profile.Name
                );

                if (!string.IsNullOrWhiteSpace(newName))
                {
                    var oldName = profile.Name;
                    profile.Name = newName;
                    profile.LastUsed = DateTime.Now;
                    
                    _configService.SaveSettings(_settings);
                    
                    
                    RecentProfiles = new ObservableCollection<InjectionProfile>(_settings.RecentProfiles);
                    Status = $"Profile renamed to '{newName}'";
                    _loggingService.Info($"Profile '{oldName}' renamed to '{newName}'", "ProfileManager");
                }
            }
        }

        private void ExecuteCopyStatusCommand(object parameter)
        {
            Clipboard.SetText(Status);
        }

        private bool CanExecuteRefreshCommand(object parameter)
        {
            return !IsRefreshing;
        }

        private async void ExecuteRefreshCommand(object parameter)
        {
            _loggingService.Info("Starting process refresh", "ProcessScanner");
            File.AppendAllText(AppDomain.CurrentDomain.BaseDirectory + "\\DebugLog.txt", "[MainWindowViewModel] - ExecuteRefresh Entered\r\n");
            IsRefreshing = true;
            Status = "Refreshing processes";
            ObservableCollection<MonoProcess> processes = new ObservableCollection<MonoProcess>();

            File.AppendAllText(AppDomain.CurrentDomain.BaseDirectory + "\\DebugLog.txt", "[MainWindowViewModel] - Setting Process Access Rights:\r\n\tPROCESS_QUERY_INFORMATION\r\n\tPROCESS_VM_READ\r\n");
            File.AppendAllText(AppDomain.CurrentDomain.BaseDirectory + "\\DebugLog.txt", "[MainWindowViewModel] - Checking Processes for Mono\r\n");

            await Task.Run(() =>
            {
                int cp = Process.GetCurrentProcess().Id;

                foreach (Process p in Process.GetProcesses())
                {
                    try
                    {
                        

                        if (ScanOnlyMonoGames && !IsMonoGameProcess(p))
                        {
                            continue; 
                        }


                        
                        var t = GetProcessUser(p);

                        if (t != null)
                        {
                            if (p.Id == cp)
                            {
                                continue;
                            }

                            const ProcessAccessRights flags = ProcessAccessRights.PROCESS_QUERY_INFORMATION | ProcessAccessRights.PROCESS_VM_READ;
                            IntPtr handle;

                            if ((handle = Native.OpenProcess(flags, false, p.Id)) != IntPtr.Zero)
                            {
                                File.AppendAllText(AppDomain.CurrentDomain.BaseDirectory + "\\DebugLog.txt", "\t" + p.ProcessName + ".exe\r\n");
                                if (ProcessUtils.GetMonoModule(handle, out IntPtr mono))
                                {
                                    File.AppendAllText(AppDomain.CurrentDomain.BaseDirectory + "\\DebugLog.txt", "\t\tMono found in process: " + p.ProcessName + ".exe\r\n");
                                    _loggingService.Success($"Found Mono process: {p.ProcessName} (PID: {p.Id})", "ProcessScanner");
                                    processes.Add(new MonoProcess
                                    {
                                        MonoModule = mono,
                                        Id = p.Id,
                                        Name = p.ProcessName
                                    });
                                }

                                Native.CloseHandle(handle);
                            }
                        }
                    }
                    catch(Exception e) 
                    { 
                        File.AppendAllText(AppDomain.CurrentDomain.BaseDirectory + "\\DebugLog.txt", "    ERROR SCANNING: " + p.ProcessName + " - " + e.Message + "\r\n");
                        _loggingService.Error($"Error scanning {p.ProcessName}: {e.Message}", "ProcessScanner");
                    }

                }

                File.AppendAllText(AppDomain.CurrentDomain.BaseDirectory + "\\DebugLog.txt", "FINISHED SCANNING PROCESSES...\r\n");
            });

            Processes = processes;

            if (Processes.Count > 0)
            {
                Status = "Processes refreshed";
                SelectedProcess = Processes[0];
                _settings.AddRecentProcess(SelectedProcess.Name);
                _loggingService.Success($"Process scan complete - {Processes.Count} Mono process(es) found", "ProcessScanner");
            }
            else
            {
                Status = "No Mono processess found!";
                _loggingService.Warning("No Mono processes found", "ProcessScanner");
                File.AppendAllText(AppDomain.CurrentDomain.BaseDirectory + "\\DebugLog.txt", "No Mono processess found:\r\n");
            }

            IsRefreshing = false;
        }

        private void ExecuteBrowseCommand(object parameter)
        {
            OpenFileDialog ofd = new OpenFileDialog();
            ofd.Filter = "Dynamic Link Library|*.dll";
            ofd.Title = "Select assembly to inject";

            if (!string.IsNullOrEmpty(_settings.LastAssemblyPath))
            {
                ofd.InitialDirectory = Path.GetDirectoryName(_settings.LastAssemblyPath);
            }

            if (ofd.ShowDialog() == true)
            {
                AssemblyPath = ofd.FileName;
                _settings.AddRecentAssembly(ofd.FileName);
                RecentAssemblies = new ObservableCollection<string>(_settings.RecentAssemblies);
            }
        }

        private bool CanExecuteInjectCommand(object parameter)
        {
            return SelectedProcess != null &&
                File.Exists(AssemblyPath) &&
                !string.IsNullOrEmpty(InjectClassName) &&
                !string.IsNullOrEmpty(InjectMethodName) &&
                !IsExecuting;
        }

        private void ExecuteInjectCommand(object parameter)
        {
            _loggingService.Info($"Starting injection: {Path.GetFileName(AssemblyPath)}", "Injector");
            
            IntPtr handle = IntPtr.Zero;
            try
            {
                handle = Native.OpenProcess(ProcessAccessRights.PROCESS_ALL_ACCESS, false, SelectedProcess.Id);

                if (handle == IntPtr.Zero)
                {
                    Status = "Failed to open process";
                    _loggingService.Error("Failed to open target process", "Injector");
                    return;
                }
            }
            catch (Exception ex)
            {
                Status = "Error: " + ex.Message;
                _loggingService.Error($"Error opening process: {ex.Message}", "Injector");
                return;
            }

            byte[] file;

            try
            {
                file = File.ReadAllBytes(AssemblyPath);
                _loggingService.Info($"Loaded assembly file ({file.Length} bytes)", "Injector");
            }
            catch (IOException ex)
            {
                Status = "Failed to read the file " + AssemblyPath;
                _loggingService.Error($"Failed to read assembly file: {ex.Message}", "Injector");
                return;
            }

            IsExecuting = true;
            Status = "Injecting " + Path.GetFileName(AssemblyPath);

            using (Injector injector = new Injector(handle, SelectedProcess.MonoModule))
            {
                injector.Options = new InjectionOptions
                {
                    RandomizeMemory = UseStealthMode,
                    HideThreads = UseStealthMode,
                    ObfuscateCode = false,
                    DelayExecution = UseStealthMode,
                    DelayMs = 150
                };

                if (UseStealthMode)
                {
                    _loggingService.Info("Stealth mode enabled", "Injector");
                }

                if (injector.IsProcessBeingDebugged())
                {
                    Status = "WARNING: Target process is being debugged!";
                    _loggingService.Warning("Target process is being debugged", "Injector");
                    System.Threading.Thread.Sleep(1000);
                }

                try
                {
                    IntPtr asm = injector.Inject(file, InjectNamespace, InjectClassName, InjectMethodName);
                    InjectedAssemblies.Add(new InjectedAssembly
                    {
                        ProcessId = SelectedProcess.Id,
                        Address = asm,
                        Name = Path.GetFileName(AssemblyPath),
                        Is64Bit = injector.Is64Bit
                    });
                    Status = "Injection successful" + (UseStealthMode ? " (STEALTH MODE)" : "");
                    _loggingService.Success($"Successfully injected {Path.GetFileName(AssemblyPath)} into {SelectedProcess.Name}", "Injector");
                    
                    SaveCurrentSettings();
                    ExecuteSaveProfileCommand(null);
                }
                catch (InjectorException ie)
                {
                    Status = "Injection failed: " + ie.Message;
                    _loggingService.Error($"Injection failed: {ie.Message}", "Injector");
                }
                catch (Exception e)
                {
                    Status = "Injection failed (unknown error): " + e.Message;
                    _loggingService.Error($"Injection failed (unknown): {e.Message}", "Injector");
                }
            }

            IsExecuting = false;
        }

        private bool CanExecuteEjectCommand(object parameter)
        {
            return SelectedAssembly != null &&
                !string.IsNullOrEmpty(EjectClassName) &&
                !string.IsNullOrEmpty(EjectMethodName) &&
                !IsExecuting;
        }

        private void ExecuteEjectCommand(object parameter)
        {
            _loggingService.Info($"Starting ejection: {SelectedAssembly.Name}", "Injector");
            
            IntPtr handle = Native.OpenProcess(ProcessAccessRights.PROCESS_ALL_ACCESS, false, SelectedAssembly.ProcessId);

            if (handle == IntPtr.Zero)
            {
                Status = "Failed to open process";
                _loggingService.Error("Failed to open target process for ejection", "Injector");
                return;
            }

            IsExecuting = true;
            Status = "Ejecting " + SelectedAssembly.Name;

            ProcessUtils.GetMonoModule(handle, out IntPtr mono);

            using (Injector injector = new Injector(handle, mono))
            {
                try
                {
                    injector.Eject(SelectedAssembly.Address, EjectNamespace, EjectClassName, EjectMethodName);
                    InjectedAssemblies.Remove(SelectedAssembly);
                    Status = "Ejection successful";
                    _loggingService.Success($"Successfully ejected {SelectedAssembly.Name}", "Injector");
                }
                catch (InjectorException ie)
                {
                    Status = "Ejection failed: " + ie.Message;
                    _loggingService.Error($"Ejection failed: {ie.Message}", "Injector");
                }
                catch (Exception e)
                {
                    Status = "Ejection failed (unknown error): " + e.Message;
                    _loggingService.Error($"Ejection failed (unknown): {e.Message}", "Injector");
                }
            }

            IsExecuting = false;
        }

        #endregion

        #region[Monitor Commands]

        private void ExecuteToggleMonitorCommand(object parameter)
        {
            try
            {
                if (IsMonitoring)
                {
                    _monitorService.Stop();
                    IsMonitoring = false;
                    Status = "⏸ Monitoring stopped";
                    _loggingService.Info("Process monitoring stopped", "Monitor");
                }
                else
                {
                    if (WatchedProcesses.Count == 0)
                    {
                        MessageBox.Show("Add at least one process to watch before starting the monitor.", "No Processes", MessageBoxButton.OK, MessageBoxImage.Information);
                        return;
                    }

                    var monitorSettings = new ProcessMonitorSettings
                    {
                        IsEnabled = true,
                        WatchedProcesses = WatchedProcesses.ToList(),
                        AutoInjectOnDetection = AutoInject,
                        ShowNotifications = ShowNotifications,
                        MonitorIntervalMs = 2000
                    };

                    _monitorService.Start(monitorSettings);
                    IsMonitoring = true;
                    Status = "▶ Monitoring active: " + string.Join(", ", WatchedProcesses.Select(w => w.ProcessName));
                    _loggingService.Info($"Process monitoring started: {string.Join(", ", WatchedProcesses.Select(w => w.ProcessName))}", "Monitor");
                }

                SaveCurrentSettings();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Monitor error: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private bool CanExecuteAddWatchProcessCommand(object parameter)
        {
            return !string.IsNullOrWhiteSpace(WatchProcessName);
        }

        private void ExecuteAddWatchProcessCommand(object parameter)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(WatchProcessName))
                    return;

                string processName = WatchProcessName.Trim().Replace(".exe", "");

                if (!WatchedProcesses.Any(w => w.ProcessName.Equals(processName, StringComparison.OrdinalIgnoreCase)))
                {
                    var watchedProcess = new WatchedProcess
                    {
                        ProcessName = processName,
                        UseCurrentSettings = true,
                        Profile = null
                    };

                    WatchedProcesses.Add(watchedProcess);
                    WatchProcessName = string.Empty;
                    SaveCurrentSettings();
                    _loggingService.Info($"Added watch process: {processName}", "Monitor");
                    Status = $"Added {processName} to watch list";
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to add watch process: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ExecuteRemoveWatchProcessCommand(object parameter)
        {
            try
            {
                if (parameter is WatchedProcess watchedProcess)
                {
                    WatchedProcesses.Remove(watchedProcess);
                    SaveCurrentSettings();
                    _loggingService.Info($"Removed watch process: {watchedProcess.ProcessName}", "Monitor");
                    Status = $"Removed {watchedProcess.ProcessName} from watch list";
                }
            }
            catch (Exception)
            {
                // Ignores on
            }
        }

        private void ExecuteSetWatchProfileCommand(object parameter)
        {
            if (parameter is WatchedProcess watchedProcess)
            {
                var vm = new SelectProfileViewModel(RecentProfiles, watchedProcess)
                {
                    Title = $"Select Profile for {watchedProcess.ProcessName}"
                };

                try
                {
                    var dialog = new SelectProfileWindow(vm);

                    if (dialog.ShowDialog() == true && vm.Result != null)
                    {
                        var originalItem = WatchedProcesses.FirstOrDefault(w => w.ProcessName == watchedProcess.ProcessName);
                        if (originalItem != null)
                        {
                            originalItem.UseCurrentSettings = vm.Result.UseCurrentSettings;
                            originalItem.Profile = vm.Result.Profile;
                            SaveCurrentSettings();
                        }
                    }
                }
                catch (Exception ex)
                {
                    string errorMsg = $"An error occurred opening the profile window. This is often a XAML styling issue.\n\nError: {ex.Message}\n\n{ex.InnerException?.Message}";
                    MessageBox.Show(errorMsg, "UI Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    _loggingService.Error($"Failed to open SelectProfileWindow: {ex}", "UI");
                }
            }
        }
        
        private async void OnProcessDetected(object sender, ProcessDetectedEventArgs e)
        {
            try
            {
                await Application.Current.Dispatcher.InvokeAsync(async () =>
                {
                    try
                    {
                        _loggingService.Success($"Detected watched process: {e.Process.Name} (PID: {e.Process.Id})", "Monitor");
                        
                        IntPtr handle = IntPtr.Zero;
                        try
                        {
                            const ProcessAccessRights flags = ProcessAccessRights.PROCESS_QUERY_INFORMATION | ProcessAccessRights.PROCESS_VM_READ;
                            handle = Native.OpenProcess(flags, false, e.Process.Id);
                            if (handle != IntPtr.Zero && ProcessUtils.GetMonoModule(handle, out IntPtr mono))
                            {
                                var monoProcess = new MonoProcess
                                {
                                    Id = e.Process.Id,
                                    Name = e.Process.Name,
                                    MonoModule = mono
                                };

                                
                                if (!Processes.Any(p => p.Id == monoProcess.Id))
                                {
                                    Processes.Add(monoProcess);
                                }
                                SelectedProcess = monoProcess;
                            }
                        }
                        catch (Exception ex)
                        {
                            _loggingService.Error($"Failed to inspect detected process {e.Process.Name}: {ex.Message}", "Monitor");
                        }
                        finally
                        {
                            if (handle != IntPtr.Zero)
                            {
                                Native.CloseHandle(handle);
                            }
                        }
                        await Task.Delay(500);

                        var targetProcess = Processes?.FirstOrDefault(p => p.Id == e.Process.Id);
                        if (targetProcess != null)
                        {
                            SelectedProcess = targetProcess;

                            if (AutoInject)
                            {
                                if (e.WatchedProcessInfo.UseCurrentSettings)
                                {
                                    if (CanExecuteInjectCommand(null))
                                    {
                                        await Task.Delay(1000);
                                        ExecuteInjectCommand(null);
                                    }
                                    else
                                    {
                                        _loggingService.Warning("Cannot auto-inject: Current settings incomplete", "Monitor");
                                        Status = "⚠ Auto-inject failed: Settings incomplete";
                                    }
                                }
                                else if (e.WatchedProcessInfo.Profile != null)
                                {
                                    LoadProfile(e.WatchedProcessInfo.Profile);
                                    await Task.Delay(500);
                                    
                                    if (CanExecuteInjectCommand(null))
                                    {
                                        await Task.Delay(1000);
                                        ExecuteInjectCommand(null);
                                    }
                                    else
                                    {
                                        _loggingService.Warning($"Cannot auto-inject: Profile '{e.WatchedProcessInfo.Profile.Name}' incomplete", "Monitor");
                                        Status = "⚠ Auto-inject failed: Profile incomplete";
                                    }
                                }
                                else
                                {
                                    _loggingService.Warning("Cannot auto-inject: No profile configured", "Monitor");
                                    Status = "⚠ Auto-inject failed: No profile configured";
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        _loggingService.Error($"Error in process detected handler: {ex.Message}", "Monitor");
                    }
                });
            }
            catch (Exception ex)
            {
                _loggingService.Error($"Outer error in process detected handler: {ex.Message}", "Monitor");
            }
        }

        public void Cleanup()
        {
            _monitorService?.Dispose();
            SaveCurrentSettings();
        }

        #endregion



        private bool _scanOnlyMonoGames;
        public bool ScanOnlyMonoGames
        {
            get => _scanOnlyMonoGames;
            set => Set(ref _scanOnlyMonoGames, value);
        }
        private bool IsMonoGameProcess(Process p)
        {
            try
            {
                string processPath = p.MainModule.FileName;
                string processDir = Path.GetDirectoryName(processPath);
                string processNameNoExt = Path.GetFileNameWithoutExtension(processPath);
                return Directory.Exists(Path.Combine(processDir, $"{processNameNoExt}_Data"));
            }
            catch
            {
                return false; 
            }
        }

        #region[Properties]

        private bool _isRefreshing;
        public bool IsRefreshing
        {
            get => _isRefreshing;
            set
            {
                Set(ref _isRefreshing, value);
                RefreshCommand.RaiseCanExecuteChanged();
            }
        }
        
        private bool _isExecuting;
        public bool IsExecuting
        {
            get => _isExecuting;
            set
            {
                Set(ref _isExecuting, value);
                InjectCommand.RaiseCanExecuteChanged();
                EjectCommand.RaiseCanExecuteChanged();
            }
        }

        private ObservableCollection<MonoProcess> _processes;
        public ObservableCollection<MonoProcess> Processes
        {
            get => _processes;
            set => Set(ref _processes, value);
        }

        private MonoProcess _selectedProcess;
        public MonoProcess SelectedProcess
        {
            get => _selectedProcess;
            set
            {
                Set(ref _selectedProcess, value);    
                InjectCommand.RaiseCanExecuteChanged();
            }
        }

        private string _status;
        public string Status
        {
            get => _status;
            set => Set(ref _status, value);
        }

        private bool _avalert;
        public bool AVAlert
        {
            get => _avalert;
            set => Set(ref _avalert, value);
        }

        private string _avcolor;
        public string AVColor
        {
            get => _avcolor;
            set => Set(ref _avcolor, value);
        }

        private string _assemblyPath;
        public string AssemblyPath
        {
            get => _assemblyPath;
            set
            {
                Set(ref _assemblyPath, value);
                if (File.Exists(_assemblyPath))
                    InjectNamespace = Path.GetFileNameWithoutExtension(_assemblyPath);
                InjectCommand.RaiseCanExecuteChanged();
                SaveProfileCommand.RaiseCanExecuteChanged();
            }
        }

        private string _injectNamespace;
        public string InjectNamespace
        {
            get => _injectNamespace;
            set
            {
                Set(ref _injectNamespace, value);
                EjectNamespace = value;
            }
        }

        private string _injectClassName;
        public string InjectClassName
        {
            get => _injectClassName;
            set
            {
                Set(ref _injectClassName, value);
                EjectClassName = value;
                InjectCommand.RaiseCanExecuteChanged();
                SaveProfileCommand.RaiseCanExecuteChanged();
            }
        }

        private string _injectMethodName;
        public string InjectMethodName
        {
            get => _injectMethodName;
            set
            {
                Set(ref _injectMethodName, value);
                if (_injectMethodName == "Load")
                    EjectMethodName = "Unload";
                InjectCommand.RaiseCanExecuteChanged();
                SaveProfileCommand.RaiseCanExecuteChanged();
            }
        }

        private ObservableCollection<InjectedAssembly> _injectedAssemblies = new ObservableCollection<InjectedAssembly>();
        public ObservableCollection<InjectedAssembly> InjectedAssemblies
        {
            get => _injectedAssemblies;
            set => Set(ref _injectedAssemblies, value);
        }

        private InjectedAssembly _selectedAssembly;
        public InjectedAssembly SelectedAssembly
        {
            get => _selectedAssembly;
            set
            {
                Set(ref _selectedAssembly, value);
                EjectCommand.RaiseCanExecuteChanged();
            }
        }

        private string _ejectNamespace;
        public string EjectNamespace
        {
            get => _ejectNamespace;
            set => Set(ref _ejectNamespace, value);
        }

        private string _ejectClassName;
        public string EjectClassName
        {
            get => _ejectClassName;
            set
            {
                Set(ref _ejectClassName, value);
                EjectCommand.RaiseCanExecuteChanged();
            }
        }

        private string _ejectMethodName;
        public string EjectMethodName
        {
            get => _ejectMethodName;
            set
            {
                Set(ref _ejectMethodName, value);
                EjectCommand.RaiseCanExecuteChanged();
            }
        }

        private bool _useStealthMode = false;
        public bool UseStealthMode
        {
            get => _useStealthMode;
            set => Set(ref _useStealthMode, value);
        }

        private ObservableCollection<string> _recentAssemblies = new ObservableCollection<string>();
        public ObservableCollection<string> RecentAssemblies
        {
            get => _recentAssemblies;
            set => Set(ref _recentAssemblies, value);
        }

        private ObservableCollection<InjectionProfile> _recentProfiles = new ObservableCollection<InjectionProfile>();
        public ObservableCollection<InjectionProfile> RecentProfiles
        {
            get => _recentProfiles;
            set => Set(ref _recentProfiles, value);
        }

        private bool _isMonitoring;
        public bool IsMonitoring
        {
            get => _isMonitoring;
            set
            {
                Set(ref _isMonitoring, value);
                MonitorButtonText = value ? "⏸ Stop Monitoring" : "▶ Start Monitoring";
            }
        }

        private string _monitorButtonText = "▶ Start Monitoring";
        public string MonitorButtonText
        {
            get => _monitorButtonText;
            set => Set(ref _monitorButtonText, value);
        }

        private bool _autoInject;
        public bool AutoInject
        {
            get => _autoInject;
            set
            {
                Set(ref _autoInject, value);
                SaveCurrentSettings();
            }
        }

        private bool _showNotifications = true;
        public bool ShowNotifications
        {
            get => _showNotifications;
            set
            {
                Set(ref _showNotifications, value);
                SaveCurrentSettings();
            }
        }

        private string _watchProcessName;
        public string WatchProcessName
        {
            get => _watchProcessName;
            set
            {
                Set(ref _watchProcessName, value);
                AddWatchProcessCommand.RaiseCanExecuteChanged();
            }
        }

        private ObservableCollection<WatchedProcess> _watchedProcesses = new ObservableCollection<WatchedProcess>();
        public ObservableCollection<WatchedProcess> WatchedProcesses
        {
            get => _watchedProcesses;
            set => Set(ref _watchedProcesses, value);
        }

        #endregion

        #region[Process Refresh Fix]

        private static string GetProcessUser(Process process)
        {
            string result = "";
            IntPtr processHandle = IntPtr.Zero;
            try
            {
                OpenProcessToken(process.Handle, 8, out processHandle);
                using (WindowsIdentity wi = new WindowsIdentity(processHandle))
                {
                    string user = wi.Name;
                    result = user.Contains(@"\") ? user.Substring(user.IndexOf(@"\") + 1) : user;
                }
            }
            catch(Exception ex)
            {
                File.AppendAllText(AppDomain.CurrentDomain.BaseDirectory + "\\DebugLog.txt", "    Error Getting User Process: " + process.ProcessName + " - " + ex.Message + "\r\n");
                return null;
            }
            finally
            {
                if (processHandle != IntPtr.Zero)
                {
                    CloseHandle(processHandle);
                }
            }

            return result;
        }

        [DllImport("advapi32.dll", SetLastError = true)]
        private static extern bool OpenProcessToken(IntPtr ProcessHandle, uint DesiredAccess, out IntPtr TokenHandle);

        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool CloseHandle(IntPtr hObject);

        #endregion

        #region[AntiVirus PreTest]

        public static bool AntivirusInstalled()
        {
            try
            {
                List<string> avs = new List<string>();
                bool defenderFlag = false;
                string wmipathstr = @"\\" + Environment.MachineName + @"\root\SecurityCenter2";

                ManagementObjectSearcher searcher = new ManagementObjectSearcher(wmipathstr, "SELECT * FROM AntivirusProduct");
                ManagementObjectCollection instances = searcher.Get();

                if (instances.Count > 0)
                {
                    File.AppendAllText(AppDomain.CurrentDomain.BaseDirectory + "\\DebugLog.txt", "AntiVirus Installed: True\r\n");

                    string installedAVs = "Installed AntiVirus':\r\n";
                    foreach(ManagementBaseObject av in instances)
                    {
                        var AVInstalled = ((string)av.GetPropertyValue("pathToSignedProductExe")).Replace("//", "") + " " + (string)av.GetPropertyValue("pathToSignedReportingExe");
                        installedAVs += "   " + AVInstalled + "\r\n";
                        avs.Add(AVInstalled.ToLower());
                    }
                    File.AppendAllText(AppDomain.CurrentDomain.BaseDirectory + "\\DebugLog.txt", installedAVs + "\r\n");
                }
                else { File.AppendAllText(AppDomain.CurrentDomain.BaseDirectory + "\\DebugLog.txt", "AntiVirus Installed: False\r\n"); }

                foreach (Process p in Process.GetProcesses())
                {
                    foreach (var detectedAV in avs)
                    {
                        if (detectedAV.EndsWith(p.ProcessName.ToLower() + ".exe"))
                        {
                            File.AppendAllText(AppDomain.CurrentDomain.BaseDirectory + "\\DebugLog.txt", "AntiVirus Running: " + detectedAV + "\r\n");
                        }
                    }
                }

                if (defenderFlag) { return false; } else { return instances.Count > 0;}                
            }
            catch (Exception e)
            {
                File.AppendAllText(AppDomain.CurrentDomain.BaseDirectory + "\\DebugLog.txt", "Error Checking for AV: " + e.Message + "\r\n");
            }

            return false;
        }

        #endregion
    }
}