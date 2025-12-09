using System.Collections.ObjectModel;
using System.Reflection;

namespace SharpMonoInjector.Gui.Models
{
    public class AssemblyNamespace
    {
        public string Name { get; set; }
        public ObservableCollection<AssemblyClass> Classes { get; set; }

        public AssemblyNamespace()
        {
            Classes = new ObservableCollection<AssemblyClass>();
        }
    }

    public class AssemblyClass
    {
        public string Name { get; set; }
        public string FullName { get; set; }
        public string Namespace { get; set; }
        public ObservableCollection<AssemblyMethod> Methods { get; set; }
        public bool IsStatic { get; set; }
        public bool IsPublic { get; set; }

        public AssemblyClass()
        {
            Methods = new ObservableCollection<AssemblyMethod>();
        }
    }

    public class AssemblyMethod
    {
        public string Name { get; set; }
        public string FullSignature { get; set; }
        public ObservableCollection<ParameterInfo> Parameters { get; set; } = new ObservableCollection<ParameterInfo>();
        public string ReturnType { get; set; }
        public bool IsStatic { get; set; }
        public bool IsPublic { get; set; }
        public bool IsConstructor { get; set; }
    }

    public class AssemblyValidationResult
    {
        public bool IsValid { get; set; }
        public string Message { get; set; }
        public ObservableCollection<string> Warnings { get; set; } = new ObservableCollection<string>();
        public ObservableCollection<string> Errors { get; set; } = new ObservableCollection<string>();
    }
}