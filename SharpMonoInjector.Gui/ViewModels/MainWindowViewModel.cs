using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.IO.Pipes;
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
    public enum BepInExType
    {
        Standard,
        ModManager,
        None
    }

    public class BepInExLocation
    {
        public BepInExType Type { get; set; }
        public string Path { get; set; }
        public bool HasReceiver { get; set; }
        public string ReceiverPath { get; set; }
    }

    public class MainWindowViewModel : ViewModel
    {
        private const string PIPE_NAME = "SharpMonoInjectorPipe_THOZE";
        private const string RECEIVER_DLL_NAME = "SharpMonoInjectorTheHolyOneZEdition.dll";
        private const string DISCORD_LINK = "discord.gg/Wp9Mf4YfTS";

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
            IsRefreshing = true;
            Status = "Refreshing processes";
            ObservableCollection<MonoProcess> processes = new ObservableCollection<MonoProcess>();

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
                                if (ProcessUtils.GetMonoModule(handle, out IntPtr mono))
                                {
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
                        _loggingService.Error($"Error scanning {p.ProcessName}: {e.Message}", "ProcessScanner");
                    }
                }
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
            Process gameProcess;
            try
            {
                gameProcess = Process.GetProcessById(SelectedProcess.Id);
            }
            catch (Exception ex)
            {
                Status = "Error: Target process not found or closed.";
                _loggingService.Error($"Failed to get process by ID {SelectedProcess.Id}: {ex.Message}", "Injector");
                return;
            }

            string gameDirectory = Path.GetDirectoryName(gameProcess.MainModule.FileName);
            string gameExecutableName = Path.GetFileNameWithoutExtension(gameProcess.MainModule.FileName);

            _loggingService.Info($"Scanning for BepInEx installations for game: {gameExecutableName}", "Injector");

            var locations = DetectAllBepInExLocations(gameDirectory, gameExecutableName);

            var standard = locations.FirstOrDefault(l => l.Type == BepInExType.Standard);
            var modManager = locations.FirstOrDefault(l => l.Type == BepInExType.ModManager);

            bool standardExists = standard != null;
            bool standardHasReceiver = standard?.HasReceiver ?? false;
            bool modManagerExists = modManager != null;
            bool modManagerHasReceiver = modManager?.HasReceiver ?? false;

            _loggingService.Info($"Detection Results: Standard={standardExists} (Receiver={standardHasReceiver}), ModManager={modManagerExists} (Receiver={modManagerHasReceiver})", "Injector");

            if (standardHasReceiver && modManagerHasReceiver)
            {
                _loggingService.Info("Both receivers detected - auto-trying both methods", "Injector");
                SmartAutoTryBoth(standard, modManager);
            }
            else if (standardHasReceiver)
            {
                _loggingService.Info("Only standard receiver detected - using it", "Injector");
                AttemptSingleMethod(standard);
            }
            else if (modManagerHasReceiver)
            {
                _loggingService.Info("Only mod manager receiver detected - using it", "Injector");
                AttemptSingleMethod(modManager);
            }
            else if (standardExists && modManagerExists)
            {
                HandleBothExistNoReceivers(standard.Path, modManager.Path);
            }
            else if (standardExists)
            {
                HandleStandardOnlyNoReceiver(standard.Path);
            }
            else if (modManagerExists)
            {
                HandleModManagerOnlyNoReceiver(modManager.Path);
            }
            else
            {
                _loggingService.Info("No BepInEx detected. Using standard injection", "Injector");
                ExecuteStandardInjection();
            }
        }

        private void SmartAutoTryBoth(BepInExLocation standard, BepInExLocation modManager)
        {
            _loggingService.Info("Trying Standard BepInEx first...", "Injector");
            
            IsExecuting = true;
            Status = "Trying Standard BepInEx...";

            bool standardSuccess = InjectViaPipe(AssemblyPath, InjectNamespace, InjectClassName, InjectMethodName);

            if (standardSuccess)
            {
                Status = "✓ Injection successful (Standard BepInEx)";
                _loggingService.Success($"Successfully injected via Standard BepInEx", "Injector");
                SaveCurrentSettings();
                ExecuteSaveProfileCommand(null);
                IsExecuting = false;
                return;
            }

            _loggingService.Info("Standard failed, trying Mod Manager...", "Injector");
            Status = "Trying Mod Manager...";

            bool modManagerSuccess = InjectViaPipe(AssemblyPath, InjectNamespace, InjectClassName, InjectMethodName);

            if (modManagerSuccess)
            {
                Status = "✓ Injection successful (Mod Manager)";
                _loggingService.Success($"Successfully injected via Mod Manager", "Injector");
                SaveCurrentSettings();
                ExecuteSaveProfileCommand(null);
                IsExecuting = false;
                return;
            }

            IsExecuting = false;
            _loggingService.Warning("Both receiver methods failed", "Injector");
            
            var result = MessageBox.Show(
                "⚠️ Both Receiver Methods Failed!\n\n" +
                "You have receivers installed in both locations, but neither responded.\n" +
                "This usually means the game wasn't started with BepInEx at all.\n\n" +
                "Would you like to try direct memory injection instead?\n" +
                "(This bypasses BepInEx entirely)\n\n" +
                $"💬 Need help? Join Discord: {DISCORD_LINK}",
                "Both Methods Failed",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (result == MessageBoxResult.Yes)
            {
                _loggingService.Info("User chose direct injection fallback", "Injector");
                ExecuteStandardInjection();
            }
            else
            {
                Status = "✗ Injection failed - restart game with BepInEx";
            }
        }

        private void AttemptSingleMethod(BepInExLocation location)
        {
            string typeStr = location.Type == BepInExType.Standard ? "Standard BepInEx" : "Mod Manager";
            
            IsExecuting = true;
            Status = $"Injecting via {typeStr}...";

            bool success = InjectViaPipe(AssemblyPath, InjectNamespace, InjectClassName, InjectMethodName);

            if (success)
            {
                Status = $"✓ Injection successful ({typeStr})";
                _loggingService.Success($"Successfully injected via {typeStr}", "Injector");
                SaveCurrentSettings();
                ExecuteSaveProfileCommand(null);
                IsExecuting = false;
                return;
            }

            IsExecuting = false;
            _loggingService.Warning($"{typeStr} receiver failed to respond", "Injector");
            
            string message;
            if (location.Type == BepInExType.ModManager)
            {
                message = "⚠️ Mod Manager Receiver Not Responding!\n\n" +
                        "The receiver plugin exists in the mod manager folder,\n" +
                        "but it's NOT loaded into the game!\n\n" +
                        "Common causes:\n" +
                        "  • Plugin is disabled in mod manager settings\n" +
                        "  • BepInEx failed to initialize\n" +
                        "  • Plugin load order issue\n" +
                        "  • Mod manager didn't start BepInEx properly\n\n" +
                        "SOLUTIONS:\n" +
                        "1. Check if the receiver is enabled in your mod manager\n" +
                        "2. Check BepInEx console for load errors\n" +
                        "3. Try restarting the game via the mod manager\n\n" +
                        "OR try direct memory injection instead?\n" +
                        "(Bypasses BepInEx entirely - always works)\n\n" +
                        $"💬 Need help? Join Discord: {DISCORD_LINK}";
            }
            else
            {
                message = $"⚠️ Standard BepInEx Receiver Not Responding!\n\n" +
                        "The receiver plugin exists but isn't responding.\n" +
                        "This usually means:\n" +
                        "  • The game wasn't started with BepInEx\n" +
                        "  • BepInEx failed to load the plugin\n\n" +
                        "Would you like to try direct memory injection instead?\n" +
                        "(This bypasses BepInEx entirely)\n\n" +
                        $"💬 Need help? Join Discord: {DISCORD_LINK}";
            }
            
            var result = MessageBox.Show(
                message,
                "Receiver Not Loaded Into Game",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (result == MessageBoxResult.Yes)
            {
                _loggingService.Info("User chose direct injection fallback", "Injector");
                ExecuteStandardInjection();
            }
            else
            {
                Status = "✗ Injection failed - check mod manager settings";
            }
        }

        private void HandleBothExistNoReceivers(string standardPath, string modManagerPath)
        {
            _loggingService.Warning("Both BepInEx installations exist but neither has receiver", "Injector");
            Status = "⚠ Multiple BepInEx detected! Receiver required.";
            
            var result = MessageBox.Show(
                "⚠️ Multiple BepInEx Installations Without Receiver!\n\n" +
                "You have BepInEx in TWO locations:\n" +
                "  • Standard (game folder)\n" +
                "  • Mod Manager (Thunderstore/r2modman)\n\n" +
                "But NEITHER has the receiver plugin!\n\n" +
                "📍 RECOMMENDED: Install in Standard BepInEx\n" +
                $"   Path: {Path.Combine(standardPath, "plugins")}\n\n" +
                "TO FIX THIS:\n" +
                $"1. Get the receiver from Discord: {DISCORD_LINK}\n" +
                $"2. Copy '{RECEIVER_DLL_NAME}'\n" +
                "3. Paste into Standard BepInEx plugins folder (recommended)\n" +
                "4. Restart the game\n\n" +
                "Do you want to try direct injection anyway? (NOT RECOMMENDED)", 
                "Receiver Plugin Required", 
                MessageBoxButton.YesNo, 
                MessageBoxImage.Warning);

            if (result == MessageBoxResult.Yes)
            {
                _loggingService.Warning("User chose to inject without receiver (risky!)", "Injector");
                ExecuteStandardInjection();
            }
            else
            {
                _loggingService.Info("User canceled - waiting for receiver installation", "Injector");
                Status = "Injection canceled - install receiver first";
            }
        }

        private void HandleStandardOnlyNoReceiver(string standardPath)
        {
            _loggingService.Warning("Standard BepInEx detected without receiver plugin", "Injector");
            Status = "⚠ BepInEx detected! Receiver plugin required.";
            
            var result = MessageBox.Show(
                "⚠️ Standard BepInEx Without Receiver Plugin!\n\n" +
                "BepInEx is installed in the game folder, but the receiver plugin is missing.\n\n" +
                "Without the receiver plugin:\n" +
                "  • Injection may crash immediately\n" +
                "  • Your mod may not work properly\n" +
                "  • Game stability is not guaranteed\n\n" +
                "TO FIX THIS:\n" +
                $"1. Get the receiver from Discord: {DISCORD_LINK}\n" +
                $"2. Copy '{RECEIVER_DLL_NAME}'\n" +
                $"3. Paste into: {Path.Combine(standardPath, "plugins")}\n" +
                "4. Restart the game\n\n" +
                "Do you want to try injecting anyway? (NOT RECOMMENDED)", 
                "Receiver Plugin Missing", 
                MessageBoxButton.YesNo, 
                MessageBoxImage.Warning);

            if (result == MessageBoxResult.Yes)
            {
                _loggingService.Warning("User chose to inject without receiver (risky!)", "Injector");
                ExecuteStandardInjection();
            }
            else
            {
                _loggingService.Info("User canceled - waiting for receiver installation", "Injector");
                Status = "Injection canceled - install receiver first";
            }
        }

        private void HandleModManagerOnlyNoReceiver(string modManagerPath)
        {
            _loggingService.Warning("Mod Manager BepInEx detected without receiver plugin", "Injector");
            Status = "⚠ Mod Manager detected! Receiver plugin required.";
            
            var result = MessageBox.Show(
                "⚠️ Mod Manager Without Receiver Plugin!\n\n" +
                "You're using a mod manager, but the receiver plugin is missing.\n\n" +
                "📍 RECOMMENDED: Install in Standard BepInEx Instead\n" +
                "   Installing in the game folder is more reliable and easier.\n\n" +
                "OPTION 1 (Recommended): Install in Standard BepInEx\n" +
                $"   1. Get receiver from Discord: {DISCORD_LINK}\n" +
                "   2. Create folder: [Game]\\BepInEx\\plugins\\\n" +
                $"   3. Copy '{RECEIVER_DLL_NAME}' there\n" +
                "   4. Restart game normally (not via mod manager)\n\n" +
                "OPTION 2: Install via Mod Manager\n" +
                "   1. Install receiver through your mod manager\n" +
                "   2. Restart game through mod manager\n\n" +
                "Do you want to try direct injection anyway? (NOT RECOMMENDED)", 
                "Receiver Plugin Missing", 
                MessageBoxButton.YesNo, 
                MessageBoxImage.Warning);

            if (result == MessageBoxResult.Yes)
            {
                _loggingService.Warning("User chose to inject without receiver (risky!)", "Injector");
                ExecuteStandardInjection();
            }
            else
            {
                _loggingService.Info("User canceled - waiting for receiver installation", "Injector");
                Status = "Injection canceled - install receiver first";
            }
        }

        private List<BepInExLocation> DetectAllBepInExLocations(string gameDirectory, string gameExecutableName)
        {
            var locations = new List<BepInExLocation>();

            string standardBepInExPath = Path.Combine(gameDirectory, "BepInEx");
            if (Directory.Exists(standardBepInExPath))
            {
                string standardPluginPath = Path.Combine(standardBepInExPath, "plugins", RECEIVER_DLL_NAME);
                bool hasReceiver = File.Exists(standardPluginPath);
                
                locations.Add(new BepInExLocation
                {
                    Type = BepInExType.Standard,
                    Path = standardBepInExPath,
                    HasReceiver = hasReceiver,
                    ReceiverPath = hasReceiver ? standardPluginPath : null
                });

                _loggingService.Info($"Standard BepInEx detected: {standardBepInExPath} (Receiver: {hasReceiver})", "Injector");
            }

            string modManagerReceiverPath = FindModManagerReceiver(gameExecutableName);
            if (!string.IsNullOrEmpty(modManagerReceiverPath))
            {
                string modManagerBepInExPath = Path.GetDirectoryName(Path.GetDirectoryName(modManagerReceiverPath));
                
                locations.Add(new BepInExLocation
                {
                    Type = BepInExType.ModManager,
                    Path = modManagerBepInExPath,
                    HasReceiver = true,
                    ReceiverPath = modManagerReceiverPath
                });

                _loggingService.Success($"Mod Manager BepInEx detected: {modManagerBepInExPath}", "Injector");
            }

            return locations;
        }

        private string FindModManagerReceiver(string gameExecutableName)
        {
            try
            {
                string localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
                string roamingAppData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
                
                string[] thunderstoreBases = new[]
                {
                    Path.Combine(localAppData, "Thunderstore Mod Manager", "DataFolder"),
                    Path.Combine(roamingAppData, "Thunderstore Mod Manager", "DataFolder"),
                    Path.Combine(roamingAppData, "r2modmanPlus-local", "profiles")
                };

                foreach (var thunderstoreBase in thunderstoreBases)
                {
                    if (!Directory.Exists(thunderstoreBase))
                    {
                        continue;
                    }

                    _loggingService.Info($"Checking mod manager location: {thunderstoreBase}", "Injector");

                    var gameFolders = Directory.GetDirectories(thunderstoreBase);
                    foreach (var gameFolder in gameFolders)
                    {
                        string gameName = Path.GetFileName(gameFolder);
                        
                        if (gameName.IndexOf(gameExecutableName, StringComparison.OrdinalIgnoreCase) < 0)
                            continue;

                        _loggingService.Info($"Found matching game folder: {gameName}", "Injector");

                        string profilesPath = Path.Combine(gameFolder, "profiles");
                        if (!Directory.Exists(profilesPath))
                            profilesPath = gameFolder;

                        if (Directory.Exists(profilesPath))
                        {
                            var profiles = Directory.GetDirectories(profilesPath);
                            foreach (var profile in profiles)
                            {
                                string profileName = Path.GetFileName(profile);
                                _loggingService.Info($"Checking profile: {profileName}", "Injector");

                                string bepinexPluginsPath = Path.Combine(profile, "BepInEx", "plugins");
                                if (!Directory.Exists(bepinexPluginsPath))
                                    continue;

                                string receiverPath = SearchForReceiverDLL(bepinexPluginsPath);
                                if (receiverPath != null)
                                {
                                    _loggingService.Success($"Found mod manager receiver: {receiverPath}", "Injector");
                                    return receiverPath;
                                }
                            }
                        }
                    }
                }

                _loggingService.Info("No mod manager receiver found", "Injector");
                return null;
            }
            catch (Exception ex)
            {
                _loggingService.Error($"Error searching mod manager: {ex.Message}", "Injector");
                return null;
            }
        }

        private string SearchForReceiverDLL(string startPath)
        {
            try
            {
                string directPath = Path.Combine(startPath, RECEIVER_DLL_NAME);
                if (File.Exists(directPath))
                    return directPath;

                foreach (var dir in Directory.GetDirectories(startPath))
                {
                    string result = SearchForReceiverDLL(dir);
                    if (result != null)
                        return result;
                }

                return null;
            }
            catch
            {
                return null;
            }
        }

        private bool InjectViaPipe(string assemblyPath, string namespaceName, string className, string methodName)
        {
            try
            {
                using (var client = new NamedPipeClientStream(".", PIPE_NAME, PipeDirection.Out))
                {
                    client.Connect(5000);

                    using (var writer = new StreamWriter(client))
                    {
                        string typeName = string.IsNullOrEmpty(namespaceName) 
                            ? className 
                            : $"{namespaceName}.{className}";
                        
                        string message = $"{assemblyPath}|{typeName}|{methodName}";
                        _loggingService.Info($"Sending pipe message: {message}", "Injector");
                        writer.Write(message);
                        writer.Flush();
                    }
                }
                return true;
            }
            catch (TimeoutException)
            {
                _loggingService.Warning("Pipe connection timed out (receiver not responding)", "Injector");
                return false;
            }
            catch (Exception e)
            {
                _loggingService.Error($"Pipe error: {e.Message}", "Injector");
                return false;
            }
        }

        private void ExecuteStandardInjection()
        {
            _loggingService.Info($"Starting standard injection: {Path.GetFileName(AssemblyPath)}", "Injector");
            
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

            using (Injector injector = new Injector(handle, SelectedProcess.MonoModule, _loggingService.Info))
            {
                injector.Options = new InjectionOptions
                {
                    RandomizeMemory = UseStealthMode,
                    HideThreads = UseStealthMode,
                    ObfuscateCode = UseStealthMode,
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

            using (Injector injector = new Injector(handle, mono, _loggingService.Info))
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
            catch
            {
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
                    string installedAVs = "Installed AntiVirus':\r\n";
                    foreach(ManagementBaseObject av in instances)
                    {
                        var AVInstalled = ((string)av.GetPropertyValue("pathToSignedProductExe")).Replace("//", "") + " " + (string)av.GetPropertyValue("pathToSignedReportingExe");
                        installedAVs += "   " + AVInstalled + "\r\n";
                        avs.Add(AVInstalled.ToLower());
                    }
                }

                foreach (Process p in Process.GetProcesses())
                {
                    foreach (var detectedAV in avs)
                    {
                        if (detectedAV.EndsWith(p.ProcessName.ToLower() + ".exe"))
                        {
                        }
                    }
                }

                if (defenderFlag) { return false; } else { return instances.Count > 0;}                
            }
            catch
            {
            }

            return false;
        }

        #endregion
    }
}