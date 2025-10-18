using System;
using System.Text;
using System.ComponentModel;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Diagnostics;
using System.Linq;
using System.Threading;

namespace SharpMonoInjector
{
    public class InjectionOptions
    {
        public bool RandomizeMemory { get; set; } = false;
        public bool HideThreads { get; set; } = false;
        public bool ObfuscateCode { get; set; } = false;
        public bool DelayExecution { get; set; } = false;
        public int DelayMs { get; set; } = 100;
        internal bool PerformAntiDebugChecks { get; set; } = false;
        internal bool PerformVmCheck { get; set; } = false;
    }

    public class Injector : IDisposable
    {
        private const string mono_get_root_domain = "mono_get_root_domain";
        private const string mono_thread_attach = "mono_thread_attach";
        private const string mono_image_open_from_data = "mono_image_open_from_data";
        private const string mono_assembly_load_from_full = "mono_assembly_load_from_full";
        private const string mono_assembly_get_image = "mono_assembly_get_image";
        private const string mono_class_from_name = "mono_class_from_name";
        private const string mono_class_get_method_from_name = "mono_class_get_method_from_name";
        private const string mono_runtime_invoke = "mono_runtime_invoke";
        private const string mono_assembly_close = "mono_assembly_close";
        private const string mono_image_strerror = "mono_image_strerror";
        private const string mono_object_get_class = "mono_object_get_class";
        private const string mono_class_get_name = "mono_class_get_name";

        private readonly Dictionary<string, IntPtr> Exports = new Dictionary<string, IntPtr>
        {
            { mono_get_root_domain, IntPtr.Zero },
            { mono_thread_attach, IntPtr.Zero },
            { mono_image_open_from_data, IntPtr.Zero },
            { mono_assembly_load_from_full, IntPtr.Zero },
            { mono_assembly_get_image, IntPtr.Zero },
            { mono_class_from_name, IntPtr.Zero },
            { mono_class_get_method_from_name, IntPtr.Zero },
            { mono_runtime_invoke, IntPtr.Zero },
            { mono_assembly_close, IntPtr.Zero },
            { mono_image_strerror, IntPtr.Zero },
            { mono_object_get_class, IntPtr.Zero },
            { mono_class_get_name, IntPtr.Zero }
        };

        private Memory _memory;
        private IntPtr _rootDomain;
        private bool _attach;
        private readonly IntPtr _handle;
        private IntPtr _mono;
        private InjectionOptions _options;
        private Random _random = new Random();
        private readonly Action<string, string> _logInfo;
        private int _nopCount = 0;

        public bool Is64Bit { get; private set; }
        public InjectionOptions Options
        {
            get => _options ?? (_options = new InjectionOptions());
            set => _options = value;
        }

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern IntPtr GetCurrentThread();

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool GetThreadContext(IntPtr hThread, ref CONTEXT64 lpContext);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool GetThreadContext(IntPtr hThread, ref CONTEXT32 lpContext);

        private const int ProcessBasicInformation = 0;

        [DllImport("ntdll.dll", SetLastError = true)]
        private static extern int NtQueryInformationProcess(IntPtr processHandle, int processInformationClass, ref PROCESS_BASIC_INFORMATION processInformation, int processInformationLength, out int returnLength);

        [StructLayout(LayoutKind.Sequential)]
        internal struct PROCESS_BASIC_INFORMATION
        {
            public IntPtr Reserved1;
            public IntPtr PebBaseAddress;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 2)]
            public IntPtr[] Reserved2;
            public IntPtr UniqueProcessId;
            public IntPtr InheritedFromUniqueProcessId;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct CONTEXT64
        {
            public ulong P1Home; public ulong P2Home; public ulong P3Home; public ulong P4Home; public ulong P5Home; public ulong P6Home;
            public uint ContextFlags; public uint MxCsr;
            public ushort SegCs; public ushort SegDs; public ushort SegEs; public ushort SegFs; public ushort SegGs; public ushort SegSs;
            public uint EFlags;
            public ulong Dr0; public ulong Dr1; public ulong Dr2; public ulong Dr3; public ulong Dr6; public ulong Dr7;
            public ulong Rax; public ulong Rcx; public ulong Rdx; public ulong Rbx; public ulong Rsp; public ulong Rbp; public ulong Rsi; public ulong Rdi;
            public ulong R8; public ulong R9; public ulong R10; public ulong R11; public ulong R12; public ulong R13; public ulong R14; public ulong R15;
            public ulong Rip;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct CONTEXT32
        {
            public uint ContextFlags;
            public uint Dr0; public uint Dr1; public uint Dr2; public uint Dr3; public uint Dr6; public uint Dr7;
            public uint SegGs; public uint SegFs; public uint SegEs; public uint SegDs;
            public uint Edi; public uint Esi; public uint Ebx; public uint Edx; public uint Ecx; public uint Eax;
            public uint Ebp; public uint Eip; public uint SegCs; public uint EFlags; public uint Esp; public uint SegSs;
        }

        private const uint CONTEXT_DEBUG_REGISTERS = 0x00010010;

        [DllImport("kernel32.dll")]
        static extern void GetSystemTimeAsFileTime(out long filetime);

        [DllImport("kernel32.dll", SetLastError = true)]
        static extern bool QueryPerformanceCounter(out long lpPerformanceCount);

        [DllImport("kernel32.dll")]
        private static extern ulong GetTickCount64();

        public Injector(string processName, Action<string, string> logInfo = null)
        {
            _logInfo = logInfo;
            if (processName.EndsWith(".exe")) { processName = processName.Replace(".exe", ""); }
            Process process = Process.GetProcesses().FirstOrDefault(p => p.ProcessName.Equals(processName, StringComparison.OrdinalIgnoreCase));

            if (process == null) throw new InjectorException($"Could not find a process with the name {processName}");
            if ((_handle = Native.OpenProcess(ProcessAccessRights.PROCESS_ALL_ACCESS, false, process.Id)) == IntPtr.Zero)
                throw new InjectorException("Failed to open process", new Win32Exception(Marshal.GetLastWin32Error()));

            Is64Bit = ProcessUtils.Is64BitProcess(_handle);
            if (!ProcessUtils.GetMonoModule(_handle, out _mono))
                throw new InjectorException("Failed to find mono.dll in the target process");

            _memory = new Memory(_handle);
        }
        public Injector(int processId, Action<string, string> logInfo = null)
        {
             _logInfo = logInfo;
            Process process = Process.GetProcesses().FirstOrDefault(p => p.Id == processId);

            if (process == null) throw new InjectorException($"Could not find a process with the id {processId}");
            if ((_handle = Native.OpenProcess(ProcessAccessRights.PROCESS_ALL_ACCESS, false, process.Id)) == IntPtr.Zero)
                throw new InjectorException("Failed to open process", new Win32Exception(Marshal.GetLastWin32Error()));
                
            Is64Bit = ProcessUtils.Is64BitProcess(_handle);
            if (!ProcessUtils.GetMonoModule(_handle, out _mono))
                throw new InjectorException("Failed to find mono.dll in the target process");

            _memory = new Memory(_handle);
        }
        public Injector(IntPtr processHandle, IntPtr monoModule, Action<string, string> logInfo = null)
        {
             _logInfo = logInfo;
            if ((_handle = processHandle) == IntPtr.Zero) throw new ArgumentException("Argument cannot be zero", nameof(processHandle));
            if ((_mono = monoModule) == IntPtr.Zero) throw new ArgumentException("Argument cannot be zero", nameof(monoModule));

            Is64Bit = ProcessUtils.Is64BitProcess(_handle);
            _memory = new Memory(_handle);
        }

        public void Dispose()
        {
            _memory?.Dispose();
            if (_handle != IntPtr.Zero) Native.CloseHandle(_handle);
        }
        private void ObtainMonoExports()
        {
            foreach (ExportedFunction ef in ProcessUtils.GetExportedFunctions(_handle, _mono))
                if (Exports.ContainsKey(ef.Name)) Exports[ef.Name] = ef.Address;
            foreach (var kvp in Exports)
                if (kvp.Value == IntPtr.Zero) throw new InjectorException($"Failed to obtain the address of {kvp.Key}()");
        }

        public bool IsProcessBeingDebugged()
        {
            bool isDebuggerPresent = false;
            Native.CheckRemoteDebuggerPresent(_handle, ref isDebuggerPresent);
            return isDebuggerPresent;
        }

        private void PerformSimpleTimingCheck()
        {
            _logInfo?.Invoke("Performing simple timing check (Sleep)...", "AntiDebug");
            long threshold = 50;
            Stopwatch sw = Stopwatch.StartNew();
            Thread.Sleep(10);
            sw.Stop();

            _logInfo?.Invoke($"Simple timing check completed in {sw.ElapsedMilliseconds}ms.", "AntiDebug");
            if (sw.ElapsedMilliseconds > threshold)
            {
                _logInfo?.Invoke($"Simple timing check failed (elapsed > threshold {threshold}ms). Potential debugger.", "AntiDebug");
                throw new InjectorException("Anti-debugging check failed (simple timing).");
            }
            _logInfo?.Invoke("Simple timing check passed.", "AntiDebug");
        }

         private void PerformRdtscTimingCheck()
        {
            _logInfo?.Invoke("Performing RDTSC timing check (using GetTickCount64 proxy)...", "AntiDebug");
            long threshold = 100;
            try
            {
                long start = (long)GetTickCount64();
                long dummy = 0;
                for(int i = 0; i < 1000; i++) { Interlocked.Increment(ref dummy); }
                long end = (long)GetTickCount64();

                long elapsed = end - start;
                _logInfo?.Invoke($"RDTSC proxy check completed in {elapsed}ms.", "AntiDebug");

                if (elapsed < 0) {
                    _logInfo?.Invoke("RDTSC proxy check resulted in negative elapsed time, skipping.", "AntiDebug");
                }
                else if (elapsed > threshold)
                {
                    _logInfo?.Invoke($"RDTSC proxy check failed (elapsed > threshold {threshold}ms). Potential debugger.", "AntiDebug");
                    throw new InjectorException("Anti-debugging check failed (RDTSC proxy timing).");
                }
                else
                {
                    _logInfo?.Invoke("RDTSC proxy check passed.", "AntiDebug");
                }
            }
            catch (Exception ex)
            {
                 _logInfo?.Invoke($"RDTSC proxy check failed with exception: {ex.Message}", "AntiDebug");
            }
        }

        private void PerformVmCheck()
        {
            _logInfo?.Invoke("Performing basic VM process check...", "AntiAnalysis");
            string[] vmProcesses = { "vmtoolsd", "vmacthlp", "vboxservice", "vboxtray" };
            HashSet<string> runningProcesses = new HashSet<string>(
                Process.GetProcesses().Select(p => p.ProcessName.ToLowerInvariant()),
                StringComparer.OrdinalIgnoreCase
            );

            foreach (string vmProc in vmProcesses)
            {
                if (runningProcesses.Contains(vmProc))
                {
                    _logInfo?.Invoke($"VM check failed. Detected running process: {vmProc}", "AntiAnalysis");
                    throw new InjectorException($"Anti-analysis check failed (VM process detected: {vmProc}).");
                }
            }
            _logInfo?.Invoke("VM process check passed.", "AntiAnalysis");
        }

        private void CheckParentProcess()
        {
            _logInfo?.Invoke("Performing parent process check...", "AntiAnalysis");
            string[] suspiciousParents = { "devenv.exe", "windbg.exe", "ollydbg.exe", "x64dbg.exe", "idaq.exe", "idaq64.exe" };
            IntPtr currentProcessHandle = Process.GetCurrentProcess().Handle;
            PROCESS_BASIC_INFORMATION pbi = new PROCESS_BASIC_INFORMATION();
            int returnLength;

            try
            {
                int status = NtQueryInformationProcess(currentProcessHandle, ProcessBasicInformation, ref pbi, Marshal.SizeOf(pbi), out returnLength);
                if (status == 0 && pbi.InheritedFromUniqueProcessId != IntPtr.Zero)
                {
                    try
                    {
                        Process parent = Process.GetProcessById(pbi.InheritedFromUniqueProcessId.ToInt32());
                        string parentName = parent.ProcessName.ToLowerInvariant() + ".exe";
                         _logInfo?.Invoke($"Parent process identified: {parentName} (PID: {parent.Id})", "AntiAnalysis");

                        if (suspiciousParents.Contains(parentName))
                        {
                             _logInfo?.Invoke($"Parent process check failed. Suspicious parent: {parentName}", "AntiAnalysis");
                             throw new InjectorException($"Anti-analysis check failed (Suspicious parent process: {parentName}).");
                        }
                         _logInfo?.Invoke("Parent process check passed.", "AntiAnalysis");
                    }
                    catch (ArgumentException)
                    {
                         _logInfo?.Invoke("Parent process has already exited.", "AntiAnalysis");
                    }
                }
                else
                {
                     _logInfo?.Invoke($"Failed to query parent process info (NtQueryInformationProcess status: {status:X}) or parent PID is zero.", "AntiAnalysis");
                }
            }
            catch (Exception ex)
            {
                _logInfo?.Invoke($"Parent process check failed with exception: {ex.Message}", "AntiAnalysis");
            }
        }

        private void CheckHardwareBreakpoints()
        {
            _logInfo?.Invoke("Performing hardware breakpoint check...", "AntiDebug");
            IntPtr hThread = GetCurrentThread();
            bool foundBreakpoint = false;

            try
            {
                if (Is64Bit)
                {
                    CONTEXT64 ctx = new CONTEXT64();
                    ctx.ContextFlags = CONTEXT_DEBUG_REGISTERS;
                    if (GetThreadContext(hThread, ref ctx))
                    {
                        if (ctx.Dr0 != 0 || ctx.Dr1 != 0 || ctx.Dr2 != 0 || ctx.Dr3 != 0)
                        {
                            foundBreakpoint = true;
                            _logInfo?.Invoke($"Hardware breakpoints detected: DR0={ctx.Dr0:X}, DR1={ctx.Dr1:X}, DR2={ctx.Dr2:X}, DR3={ctx.Dr3:X}", "AntiDebug");
                        }
                    } else {
                         _logInfo?.Invoke($"GetThreadContext (x64) failed. Error code: {Marshal.GetLastWin32Error()}", "AntiDebug");
                    }
                }
                else
                {
                    CONTEXT32 ctx = new CONTEXT32();
                    ctx.ContextFlags = CONTEXT_DEBUG_REGISTERS;
                     if (GetThreadContext(hThread, ref ctx))
                    {
                         if (ctx.Dr0 != 0 || ctx.Dr1 != 0 || ctx.Dr2 != 0 || ctx.Dr3 != 0)
                        {
                            foundBreakpoint = true;
                            _logInfo?.Invoke($"Hardware breakpoints detected: DR0={ctx.Dr0:X}, DR1={ctx.Dr1:X}, DR2={ctx.Dr2:X}, DR3={ctx.Dr3:X}", "AntiDebug");
                        }
                    } else {
                         _logInfo?.Invoke($"GetThreadContext (x86) failed. Error code: {Marshal.GetLastWin32Error()}", "AntiDebug");
                    }
                }

                if (foundBreakpoint)
                {
                    _logInfo?.Invoke("Hardware breakpoint check failed. Debug registers (DR0-DR3) appear to be set.", "AntiDebug");
                    throw new InjectorException("Anti-debugging check failed (Hardware breakpoints detected).");
                }
                 _logInfo?.Invoke("Hardware breakpoint check passed.", "AntiDebug");
            }
            catch (Exception ex)
            {
                 _logInfo?.Invoke($"Hardware breakpoint check failed with exception: {ex.Message}", "AntiDebug");
            }
        }


        public IntPtr Inject(byte[] rawAssembly, string @namespace, string className, string methodName)
        {
            if (rawAssembly == null) throw new ArgumentNullException(nameof(rawAssembly));
            if (rawAssembly.Length == 0) throw new ArgumentException($"{nameof(rawAssembly)} cannot be empty", nameof(rawAssembly));
            if (className == null) throw new ArgumentNullException(nameof(className));
            if (methodName == null) throw new ArgumentNullException(nameof(methodName));

             bool runStealthChecks = Options.DelayExecution || Options.HideThreads || Options.RandomizeMemory || Options.ObfuscateCode;
             Options.PerformAntiDebugChecks = runStealthChecks;
             Options.PerformVmCheck = runStealthChecks;

            if (runStealthChecks)
            {
                 _logInfo?.Invoke("Stealth mode checks enabled.", "Injector");

                 if (IsProcessBeingDebugged())
                 {
                     _logInfo?.Invoke("Debugger check failed (IsDebuggerPresent == true).", "AntiDebug");
                     throw new InjectorException("Anti-debugging check failed (IsDebuggerPresent).");
                 }
                 _logInfo?.Invoke("Debugger check passed (IsDebuggerPresent == false).", "AntiDebug");

                 PerformSimpleTimingCheck();
                 CheckHardwareBreakpoints();
                 CheckParentProcess();
                 PerformVmCheck();
            } else {
                 _logInfo?.Invoke("Stealth mode checks disabled.", "Injector");
            }


            if (Options.DelayExecution && Options.DelayMs > 0)
            {
                _logInfo?.Invoke($"Applying pre-injection delay: {Options.DelayMs}ms", "Injector");
                System.Threading.Thread.Sleep(Options.DelayMs);
            }

            IntPtr rawImage, assembly, image, @class, method;
            ObtainMonoExports();
            _rootDomain = GetRootDomain();
            rawImage = OpenImageFromData(rawAssembly);
            _attach = true;
            assembly = OpenAssemblyFromImage(rawImage);
            image = GetImageFromAssembly(assembly);
            @class = GetClassFromName(image, @namespace, className);
            method = GetMethodFromName(@class, methodName);
            RuntimeInvoke(method);
            return assembly;
        }

        public void Eject(IntPtr assembly, string @namespace, string className, string methodName)
        {
            if (assembly == IntPtr.Zero) throw new ArgumentException($"{nameof(assembly)} cannot be zero", nameof(assembly));
            if (className == null) throw new ArgumentNullException(nameof(className));
            if (methodName == null) throw new ArgumentNullException(nameof(methodName));

            IntPtr image, @class, method;
            ObtainMonoExports();
            _rootDomain = GetRootDomain();
            _attach = true;
            image = GetImageFromAssembly(assembly);
            @class = GetClassFromName(image, @namespace, className);
            method = GetMethodFromName(@class, methodName);
            RuntimeInvoke(method);
            CloseAssembly(assembly);
        }

        private static void ThrowIfNull(IntPtr ptr, string methodName) { if (ptr == IntPtr.Zero) throw new InjectorException($"{methodName}() returned NULL"); }

        private IntPtr GetRootDomain() {
             IntPtr rootDomain = Execute(Exports[mono_get_root_domain]);
             ThrowIfNull(rootDomain, mono_get_root_domain);
             return rootDomain;
         }

        private IntPtr OpenImageFromData(byte[] assembly)
        {
            IntPtr statusPtr = IntPtr.Zero;
            IntPtr assemblyDataPtr = IntPtr.Zero;
            IntPtr rawImage = IntPtr.Zero;
            IntPtr errorMsgPtr = IntPtr.Zero;
            string errorMessage = "Unknown image open error";
            MonoImageOpenStatus status = MonoImageOpenStatus.MONO_IMAGE_OK;

            try
            {
                statusPtr = _memory.Allocate(4);
                assemblyDataPtr = _memory.AllocateAndWrite(assembly);
                rawImage = Execute(Exports[mono_image_open_from_data],
                                           assemblyDataPtr,
                                           (IntPtr)assembly.Length,
                                           (IntPtr)1,
                                           statusPtr);

                status = (MonoImageOpenStatus)_memory.ReadInt(statusPtr);

                if (status != MonoImageOpenStatus.MONO_IMAGE_OK) {
                    errorMsgPtr = Execute(Exports[mono_image_strerror], (IntPtr)status);
                    if(errorMsgPtr != IntPtr.Zero) {
                        errorMessage = _memory.ReadString(errorMsgPtr, 256, Encoding.UTF8);
                    }
                    throw new InjectorException($"{mono_image_open_from_data}() failed: {errorMessage} (Status: {status})");
                }
                return rawImage;
            }
            finally
            {
                 // Implicitly freed by _memory.Dispose()
            }
        }


        private IntPtr OpenAssemblyFromImage(IntPtr image)
        {
            IntPtr statusPtr = IntPtr.Zero;
            IntPtr filenamePtr = IntPtr.Zero;
            IntPtr assembly = IntPtr.Zero;
            IntPtr errorMsgPtr = IntPtr.Zero;
            string errorMessage = "Unknown assembly load error";
            MonoImageOpenStatus status = MonoImageOpenStatus.MONO_IMAGE_OK;

            try
            {
                statusPtr = _memory.Allocate(4);
                filenamePtr = _memory.AllocateAndWrite(new byte[1]);
                assembly = Execute(Exports[mono_assembly_load_from_full],
                                           image,
                                           filenamePtr,
                                           statusPtr,
                                           IntPtr.Zero);

                status = (MonoImageOpenStatus)_memory.ReadInt(statusPtr);

                if (status != MonoImageOpenStatus.MONO_IMAGE_OK) {
                    errorMsgPtr = Execute(Exports[mono_image_strerror], (IntPtr)status);
                     if(errorMsgPtr != IntPtr.Zero) {
                         errorMessage = _memory.ReadString(errorMsgPtr, 256, Encoding.UTF8);
                     }
                    throw new InjectorException($"{mono_assembly_load_from_full}() failed: {errorMessage} (Status: {status})");
                }
                return assembly;
            } finally {
                 // Implicitly freed by _memory.Dispose()
            }
        }

        private IntPtr GetImageFromAssembly(IntPtr assembly) {
            IntPtr image = Execute(Exports[mono_assembly_get_image], assembly);
            ThrowIfNull(image, mono_assembly_get_image);
            return image;
        }

        private IntPtr GetClassFromName(IntPtr image, string @namespace, string className) {
             IntPtr namespacePtr = IntPtr.Zero;
             IntPtr classNamePtr = IntPtr.Zero;
             IntPtr @class = IntPtr.Zero;
             try {
                namespacePtr = _memory.AllocateAndWrite(@namespace);
                classNamePtr = _memory.AllocateAndWrite(className);
                @class = Execute(Exports[mono_class_from_name], image, namespacePtr, classNamePtr);
                ThrowIfNull(@class, mono_class_from_name);
                return @class;
             } finally {
                 // Implicitly freed by _memory.Dispose()
             }
        }

        private IntPtr GetMethodFromName(IntPtr @class, string methodName) {
            IntPtr methodNamePtr = IntPtr.Zero;
            IntPtr method = IntPtr.Zero;
            try {
                methodNamePtr = _memory.AllocateAndWrite(methodName);
                method = Execute(Exports[mono_class_get_method_from_name], @class, methodNamePtr, IntPtr.Zero);
                ThrowIfNull(method, mono_class_get_method_from_name);
                return method;
            } finally {
                // Implicitly freed by _memory.Dispose()
            }
        }

        private string GetClassName(IntPtr monoObject) {
             IntPtr @class = Execute(Exports[mono_object_get_class], monoObject);
             ThrowIfNull(@class, mono_object_get_class);
             IntPtr classNamePtr = Execute(Exports[mono_class_get_name], @class);
             ThrowIfNull(classNamePtr, mono_class_get_name);
             return _memory.ReadString(classNamePtr, 256, Encoding.UTF8);
         }

        private string ReadMonoString(IntPtr monoString) {
             int len = _memory.ReadInt(monoString + (Is64Bit ? 0x10 : 0x8));
             return _memory.ReadUnicodeString(monoString + (Is64Bit ? 0x14 : 0xC), len * 2);
        }


        private void RuntimeInvoke(IntPtr method)
        {
            IntPtr excPtrStorage = IntPtr.Zero;
            IntPtr exc = IntPtr.Zero;
            try
            {
                excPtrStorage = _memory.Allocate(IntPtr.Size);
                Execute(Exports[mono_runtime_invoke], method, IntPtr.Zero, IntPtr.Zero, excPtrStorage);
                exc = Is64Bit ? (IntPtr)_memory.ReadLong(excPtrStorage) : (IntPtr)_memory.ReadInt(excPtrStorage);

                if (exc != IntPtr.Zero)
                {
                    string className = "UnknownException";
                    string message = "No message available";
                    try { className = GetClassName(exc); } catch { }
                    try {
                        int messageOffset = Is64Bit ? 0x20 : 0x10;
                        IntPtr messageStringPtr = Is64Bit ? (IntPtr)_memory.ReadLong(exc + messageOffset) : (IntPtr)_memory.ReadInt(exc + messageOffset);
                        if (messageStringPtr != IntPtr.Zero) {
                            message = ReadMonoString(messageStringPtr);
                        }
                    } catch { }

                    throw new InjectorException($"The managed method threw an exception: ({className}) {message}");
                }
            } finally {
                 // Implicitly freed by _memory.Dispose()
            }
        }

        private void CloseAssembly(IntPtr assembly) {
             Execute(Exports[mono_assembly_close], assembly);
         }


        private IntPtr Execute(IntPtr address, params IntPtr[] args)
        {
            IntPtr retValPtr = IntPtr.Zero;
            IntPtr alloc = IntPtr.Zero;
            IntPtr thread = IntPtr.Zero;
            byte[] code = null;

            try
            {
                retValPtr = _memory.Allocate(IntPtr.Size);
                code = Assemble(address, retValPtr, args);

                if (_nopCount > 0) { _logInfo?.Invoke($"NOP padding applied: {_nopCount} bytes", "Injector"); _nopCount = 0; }
                if (Options.ObfuscateCode) { _logInfo?.Invoke("ObfuscateCode option was true, but logic is disabled.", "Injector"); }

                alloc = _memory.AllocateAndWrite(code);
                ThreadCreationFlags creationFlags = ThreadCreationFlags.None;

                thread = Native.CreateRemoteThread(_handle, IntPtr.Zero, 0, alloc, IntPtr.Zero, creationFlags, out _);
                if (thread == IntPtr.Zero) throw new InjectorException("Failed to create a remote thread", new Win32Exception(Marshal.GetLastWin32Error()));

                if (Options.HideThreads) {
                    _logInfo?.Invoke("Applying thread hiding (NtSetInformationThread)...", "Injector");
                    try { Native.NtSetInformationThread(thread, 0x11, IntPtr.Zero, 0); }
                    catch (Exception ex) { _logInfo?.Invoke($"Failed to hide thread: {ex.Message}", "Injector"); }
                } else { _logInfo?.Invoke("HideThreads option is false, skipping.", "Injector"); }

                WaitResult result = Native.WaitForSingleObject(thread, -1);
                if (result == WaitResult.WAIT_FAILED) throw new InjectorException("Failed to wait for a remote thread", new Win32Exception(Marshal.GetLastWin32Error()));

                IntPtr ret = Is64Bit ? (IntPtr)_memory.ReadLong(retValPtr) : (IntPtr)_memory.ReadInt(retValPtr);

                if ((long)ret == unchecked((long)0xC0000005))
                {
                     string funcName = Exports.FirstOrDefault(e => e.Value == address).Key ?? "Unknown Function";
                     throw new InjectorException($"An access violation occurred while executing {funcName}()");
                }
                return ret;
            }
            finally
            {
                 if(thread != IntPtr.Zero) Native.CloseHandle(thread);
                 
            }
        }

        private byte[] ObfuscateShellcode(byte[] code) { return code; }

        private byte[] Assemble(IntPtr functionPtr, IntPtr retValPtr, IntPtr[] args) { return Is64Bit ? Assemble64(functionPtr, retValPtr, args) : Assemble86(functionPtr, retValPtr, args); }
        private byte[] Assemble86(IntPtr functionPtr, IntPtr retValPtr, IntPtr[] args)
        {
            Assembler asm = new Assembler();
            if(Options.RandomizeMemory) { int p = _random.Next(4,64); _nopCount=p; for(int i=0;i<p;i++) asm.Nop(); }
            if(_attach){ asm.Push(_rootDomain); asm.MovEax(Exports[mono_thread_attach]); asm.CallEax(); asm.AddEsp(4); }
            for(int i=args.Length-1; i>=0; i--) asm.Push(args[i]);
            asm.MovEax(functionPtr);
            asm.CallEax();
            asm.AddEsp((byte)(args.Length * 4));
            asm.MovEaxTo(retValPtr);
            asm.Return();
            return asm.ToByteArray();
        }
        private byte[] Assemble64(IntPtr functionPtr, IntPtr retValPtr, IntPtr[] args)
        {
             Assembler asm = new Assembler();
             if(Options.RandomizeMemory) { int p = _random.Next(4,64); _nopCount=p; for(int i=0;i<p;i++) asm.Nop(); }
             asm.SubRsp(40);
             if(_attach) { asm.MovRax(Exports[mono_thread_attach]); asm.MovRcx(_rootDomain); asm.CallRax(); }
             asm.MovRax(functionPtr);
             for(int i=0; i<args.Length; i++) {
                 switch(i){
                     case 0: asm.MovRcx(args[i]); break;
                     case 1: asm.MovRdx(args[i]); break;
                     case 2: asm.MovR8(args[i]); break;
                     case 3: asm.MovR9(args[i]); break;
                 }
             }
             asm.CallRax();
             asm.AddRsp(40);
             asm.MovRaxTo(retValPtr);
             asm.Return();
             return asm.ToByteArray();
        }

    }
}