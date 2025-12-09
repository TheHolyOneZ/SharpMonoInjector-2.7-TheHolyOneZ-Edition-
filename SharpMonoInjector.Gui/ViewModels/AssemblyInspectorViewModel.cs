using SharpMonoInjector.Gui.Models;
using SharpMonoInjector.Gui.Services;
using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows;
using Action = System.Action<string, string>;

namespace SharpMonoInjector.Gui.ViewModels
{
    public class AssemblyInspectorViewModel : ViewModel
    {
        private readonly LoggingService _loggingService;

        private string _assemblyPath;
        public string AssemblyPath
        {
            get => _assemblyPath;
            set => Set(ref _assemblyPath, value);
        }

        private AssemblyNamespace _selectedNamespace;
        public AssemblyNamespace SelectedNamespace
        {
            get => _selectedNamespace;
            set
            {
                Set(ref _selectedNamespace, value);
                SelectedClass = null; // Clear selected class when namespace changes
                SelectedMethod = null; // Clear selected method when namespace changes
            }
        }

        private AssemblyClass _selectedClass;
        public AssemblyClass SelectedClass
        {
            get => _selectedClass;
            set => Set(ref _selectedClass, value);
        }

        private AssemblyMethod _selectedMethod;
        public AssemblyMethod SelectedMethod
        {
            get => _selectedMethod;
            set
            {
                Set(ref _selectedMethod, value);
                SelectMethodCommand.RaiseCanExecuteChanged();
            }
        }

        public ObservableCollection<AssemblyNamespace> Namespaces { get; } = new ObservableCollection<AssemblyNamespace>();
        public AssemblyValidationResult ValidationResult { get; private set; }

        public RelayCommand LoadAssemblyCommand { get; }
        public RelayCommand SelectMethodCommand { get; }
        public RelayCommand CloseCommand { get; }

        public AssemblyInspectorViewModel()
        {
            _loggingService = LoggingService.Instance;
            LoadAssemblyCommand = new RelayCommand(ExecuteLoadAssembly, CanExecuteLoadAssembly);
            SelectMethodCommand = new RelayCommand(ExecuteSelectMethod, CanExecuteSelectMethod);
            CloseCommand = new RelayCommand(ExecuteClose);

            // Initialize with empty collections to prevent binding errors
            Namespaces = new ObservableCollection<AssemblyNamespace>();
            ValidationResult = new AssemblyValidationResult { IsValid = true, Message = "Ready to load assembly" };
        }

        private bool CanExecuteLoadAssembly(object parameter)
        {
            return !string.IsNullOrEmpty(AssemblyPath) && File.Exists(AssemblyPath);
        }

        private void ExecuteLoadAssembly(object parameter)
        {
            _loggingService.Info($"Starting assembly inspection for: {AssemblyPath}", "AssemblyInspector");

            try
            {
                Namespaces.Clear();
                _loggingService.Info("Cleared previous namespaces", "AssemblyInspector");

                ValidationResult = ValidateAssembly(AssemblyPath);
                _loggingService.Info($"Assembly validation result: {ValidationResult.IsValid} - {ValidationResult.Message}", "AssemblyInspector");

                if (!ValidationResult.IsValid)
                {
                    _loggingService.Warning($"Assembly validation failed: {ValidationResult.Message}", "AssemblyInspector");
                    MessageBox.Show($"Assembly validation failed:\n{ValidationResult.Message}",
                        "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                _loggingService.Info("Loading assembly for inspection", "AssemblyInspector");

                // Set up assembly resolve handler for missing dependencies
                ResolveEventHandler resolveHandler = (sender, args) =>
                {
                    _loggingService.Info($"Attempting to resolve dependency: {args.Name}", "AssemblyInspector");

                    try
                    {
                        // Try to load the dependency in reflection-only mode
                        var dependency = Assembly.ReflectionOnlyLoad(args.Name);
                        _loggingService.Success($"Successfully resolved dependency: {args.Name}", "AssemblyInspector");
                        return dependency;
                    }
                    catch (Exception ex)
                    {
                        _loggingService.Warning($"Failed to resolve dependency {args.Name}: {ex.Message}", "AssemblyInspector");
                        return null;
                    }
                };

                AppDomain.CurrentDomain.ReflectionOnlyAssemblyResolve += resolveHandler;

                try
                {
                    Assembly assembly;
                    try
                    {
                        // Try reflection-only load first
                        assembly = Assembly.ReflectionOnlyLoadFrom(AssemblyPath);
                        _loggingService.Success($"Assembly loaded successfully: {assembly.FullName}", "AssemblyInspector");
                    }
                    catch (Exception ex)
                    {
                        _loggingService.Warning($"Reflection-only load failed, trying regular load: {ex.Message}", "AssemblyInspector");
                        // Fallback to regular load if reflection-only fails
                        assembly = Assembly.LoadFrom(AssemblyPath);
                        _loggingService.Success($"Assembly loaded with regular load: {assembly.FullName}", "AssemblyInspector");
                    }

                    ScanAssembly(assembly);
                    _loggingService.Success($"Assembly scan complete. Found {Namespaces.Count} namespaces", "AssemblyInspector");
                }
                finally
                {
                    // Clean up the event handler
                    AppDomain.CurrentDomain.ReflectionOnlyAssemblyResolve -= resolveHandler;
                }
            }
            catch (Exception ex)
            {
                _loggingService.Error($"Failed to load assembly: {ex.Message}", "AssemblyInspector");
                MessageBox.Show($"Failed to load assembly: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private AssemblyValidationResult ValidateAssembly(string path)
        {
            var result = new AssemblyValidationResult { IsValid = true };

            try
            {
                // Check if file exists
                if (!File.Exists(path))
                {
                    result.IsValid = false;
                    result.Message = "Assembly file does not exist.";
                    return result;
                }

                // Check file extension
                if (!path.EndsWith(".dll", StringComparison.OrdinalIgnoreCase) &&
                    !path.EndsWith(".exe", StringComparison.OrdinalIgnoreCase))
                {
                    result.IsValid = false;
                    result.Message = "File must be a .dll or .exe assembly.";
                    return result;
                }

                // Try to load assembly metadata
                var assemblyName = AssemblyName.GetAssemblyName(path);

                // Check if it's a .NET assembly
                if (assemblyName.ProcessorArchitecture == ProcessorArchitecture.None)
                {
                    result.Warnings.Add("Assembly architecture not specified - may not be compatible with target process.");
                }

                // Check for common issues
                var fileInfo = new FileInfo(path);
                if (fileInfo.Length == 0)
                {
                    result.IsValid = false;
                    result.Message = "Assembly file is empty.";
                    return result;
                }

                result.Message = "Assembly appears valid for injection.";
            }
            catch (BadImageFormatException)
            {
                result.IsValid = false;
                result.Message = "File is not a valid .NET assembly.";
            }
            catch (Exception ex)
            {
                result.IsValid = false;
                result.Message = $"Failed to validate assembly: {ex.Message}";
            }

            return result;
        }

        private void ScanAssembly(Assembly assembly)
        {
            _loggingService.Info("Starting assembly scan", "AssemblyInspector");

            Type[] types;
            try
            {
                types = assembly.GetTypes();
                _loggingService.Info($"Retrieved {types.Length} types from assembly", "AssemblyInspector");
            }
            catch (ReflectionTypeLoadException ex)
            {
                // Some types failed to load due to missing dependencies, but we can work with what we have
                types = ex.Types;
                _loggingService.Warning($"ReflectionTypeLoadException: {ex.Types.Length} types total, {ex.Types.Count(t => t != null)} loaded successfully, {ex.LoaderExceptions.Length} exceptions", "AssemblyInspector");

                // Log some of the loader exceptions for debugging
                for (int i = 0; i < Math.Min(3, ex.LoaderExceptions.Length); i++)
                {
                    _loggingService.Warning($"Loader exception {i + 1}: {ex.LoaderExceptions[i].Message}", "AssemblyInspector");
                }
            }

            // Be more inclusive - include all types, not just public ones
            var allTypes = types.Where(t => t != null).ToArray();
            _loggingService.Info($"Processing {allTypes.Length} valid types", "AssemblyInspector");

            if (allTypes.Length == 0)
            {
                _loggingService.Warning("No types could be loaded from assembly - this may be due to missing dependencies", "AssemblyInspector");

                // Create a placeholder namespace to show that the assembly was loaded but has issues
                var errorNs = new AssemblyNamespace { Name = "Assembly Info (Limited)" };
                var errorClass = new AssemblyClass
                {
                    Name = "AssemblyLoadError",
                    FullName = "AssemblyLoadError",
                    Namespace = "Assembly Info",
                    IsStatic = false,
                    IsPublic = true
                };

                var errorMethod = new AssemblyMethod
                {
                    Name = "Note",
                    ReturnType = "void",
                    IsStatic = false,
                    IsPublic = true,
                    IsConstructor = false,
                    FullSignature = "Note: Assembly loaded but types unavailable due to missing dependencies"
                };

                errorClass.Methods.Add(errorMethod);
                errorNs.Classes.Add(errorClass);
                Namespaces.Add(errorNs);

                _loggingService.Info("Added error information placeholder", "AssemblyInspector");
                return;
            }

            var namespaceGroups = allTypes
                .GroupBy(t => t.Namespace ?? "Global")
                .OrderBy(g => g.Key);

            _loggingService.Info($"Grouped types into {namespaceGroups.Count()} namespaces", "AssemblyInspector");

            foreach (var group in namespaceGroups)
            {
                var ns = new AssemblyNamespace { Name = group.Key };

                foreach (var type in group.OrderBy(t => t.Name))
                {
                    var classInfo = new AssemblyClass
                    {
                        Name = type.Name,
                        FullName = type.FullName,
                        Namespace = type.Namespace,
                        IsStatic = type.IsAbstract && type.IsSealed,
                        IsPublic = type.IsPublic
                    };

                    // Get methods
                    var methods = type.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly)
                        .Where(m => !m.IsSpecialName) // Exclude property getters/setters
                        .OrderBy(m => m.Name);

                    foreach (var method in methods)
                    {
                        var methodInfo = new AssemblyMethod
                        {
                            Name = method.Name,
                            ReturnType = method.ReturnType.Name,
                            IsStatic = method.IsStatic,
                            IsPublic = method.IsPublic,
                            IsConstructor = false
                        };

                        // Build signature
                        var paramTypes = method.GetParameters().Select(p => p.ParameterType.Name).ToArray();
                        methodInfo.FullSignature = $"{method.ReturnType.Name} {method.Name}({string.Join(", ", paramTypes)})";

                        // Add parameters
                        foreach (var param in method.GetParameters())
                        {
                            methodInfo.Parameters.Add(param);
                        }

                        classInfo.Methods.Add(methodInfo);
                    }

                    // Get constructors
                    var constructors = type.GetConstructors(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly);
                    foreach (var ctor in constructors)
                    {
                        var ctorInfo = new AssemblyMethod
                        {
                            Name = ".ctor",
                            ReturnType = "void",
                            IsStatic = false,
                            IsPublic = ctor.IsPublic,
                            IsConstructor = true
                        };

                        var paramTypes = ctor.GetParameters().Select(p => p.ParameterType.Name).ToArray();
                        ctorInfo.FullSignature = $"{type.Name}({string.Join(", ", paramTypes)})";

                        foreach (var param in ctor.GetParameters())
                        {
                            ctorInfo.Parameters.Add(param);
                        }

                        classInfo.Methods.Add(ctorInfo);
                    }

                    if (classInfo.Methods.Any())
                    {
                        ns.Classes.Add(classInfo);
                    }
                }

                if (ns.Classes.Any())
                {
                    Namespaces.Add(ns);
                }
            }
        }

        private bool CanExecuteSelectMethod(object parameter)
        {
            return SelectedMethod != null;
        }

        private void ExecuteSelectMethod(object parameter)
        {
            if (SelectedMethod != null)
            {
                _loggingService.Info($"Selected method for injection: {SelectedClass?.FullName}.{SelectedMethod.Name}", "AssemblyInspector");
            }

            // This will be handled by the parent window to populate the injection fields
            if (parameter is Window window)
            {
                window.DialogResult = true;
                window.Close();
            }
        }

        private void ExecuteClose(object parameter)
        {
            _loggingService.Info("Assembly inspector closed", "AssemblyInspector");
            if (parameter is Window window)
            {
                window.DialogResult = false;
                window.Close();
            }
        }
    }
}