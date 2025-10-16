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
using System.Collections.Generic;
using System.Linq;

namespace SharpMonoInjector.Gui.ViewModels
{
    public class MainWindowViewModel : ViewModel
    {
        public MainWindowViewModel()
        {
            AVAlert = AntivirusInstalled();
            if (AVAlert) { AVColor = "#FFA00668"; } else { AVColor = "#FF21AC40"; }

            RefreshCommand = new RelayCommand(ExecuteRefreshCommand, CanExecuteRefreshCommand);
            BrowseCommand = new RelayCommand(ExecuteBrowseCommand);
            InjectCommand = new RelayCommand(ExecuteInjectCommand, CanExecuteInjectCommand);
            EjectCommand = new RelayCommand(ExecuteEjectCommand, CanExecuteEjectCommand);
            CopyStatusCommand = new RelayCommand(ExecuteCopyStatusCommand);
        }

        #region[Commands]

        public RelayCommand RefreshCommand { get; }

        public RelayCommand BrowseCommand { get; }

        public RelayCommand InjectCommand { get; }

        public RelayCommand EjectCommand { get; }

        public RelayCommand CopyStatusCommand { get; }

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
                                    processes.Add(new MonoProcess
                                    {
                                        MonoModule = mono,
                                        Id = p.Id,
                                        Name = p.ProcessName
                                    });

                                    break;
                                }

                                Native.CloseHandle(handle);
                            }
                        }
                    }
                    catch(Exception e) { File.AppendAllText(AppDomain.CurrentDomain.BaseDirectory + "\\DebugLog.txt", "    ERROR SCANNING: " + p.ProcessName + " - " + e.Message + "\r\n"); }

                }

                File.AppendAllText(AppDomain.CurrentDomain.BaseDirectory + "\\DebugLog.txt", "FINISHED SCANNING PROCESSES...\r\n");
            });

            Processes = processes;

            if (Processes.Count > 0)
            {
                Status = "Processes refreshed";
                SelectedProcess = Processes[0];
            }
            else
            {
                Status = "No Mono processess found!";
                File.AppendAllText(AppDomain.CurrentDomain.BaseDirectory + "\\DebugLog.txt", "No Mono processess found:\r\n");
            }

            IsRefreshing = false;
        }

        private void ExecuteBrowseCommand(object parameter)
        {
            OpenFileDialog ofd = new OpenFileDialog();
            ofd.Filter = "Dynamic Link Library|*.dll";
            ofd.Title = "Select assembly to inject";

            if (ofd.ShowDialog() == true)
                AssemblyPath = ofd.FileName;
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
            IntPtr handle = IntPtr.Zero;
            try
            {
                handle = Native.OpenProcess(ProcessAccessRights.PROCESS_ALL_ACCESS, false, SelectedProcess.Id);

                if (handle == IntPtr.Zero)
                {
                    Status = "Failed to open process";
                    return;
                }
            }
            catch (Exception ex)
            {
                Status = "Error: " + ex.Message;
                return;
            }

            byte[] file;

            try
            {
                file = File.ReadAllBytes(AssemblyPath);
            }
            catch (IOException)
            {
                Status = "Failed to read the file " + AssemblyPath;
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

                if (injector.IsProcessBeingDebugged())
                {
                    Status = "WARNING: Target process is being debugged!";
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
                }
                catch (InjectorException ie)
                {
                    Status = "Injection failed: " + ie.Message;
                }
                catch (Exception e)
                {
                    Status = "Injection failed (unknown error): " + e.Message;
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
            IntPtr handle = Native.OpenProcess(ProcessAccessRights.PROCESS_ALL_ACCESS, false, SelectedAssembly.ProcessId);

            if (handle == IntPtr.Zero)
            {
                Status = "Failed to open process";
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
                }
                catch (InjectorException ie)
                {
                    Status = "Ejection failed: " + ie.Message;
                }
                catch (Exception e)
                {
                    Status = "Ejection failed (unknown error): " + e.Message;
                }
            }

            IsExecuting = false;
        }

        #endregion

        #region[XML Props]

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