using System;

namespace SharpMonoInjector.Gui.Models
{
    [Serializable]
    public class InjectionProfile
    {
        public string Name { get; set; }
        public string AssemblyPath { get; set; }
        public string Namespace { get; set; }
        public string ClassName { get; set; }
        public string MethodName { get; set; }
        public string EjectNamespace { get; set; }
        public string EjectClassName { get; set; }
        public string EjectMethodName { get; set; }
        public bool UseStealthMode { get; set; }
        public DateTime LastUsed { get; set; }

        public InjectionProfile()
        {
            LastUsed = DateTime.Now;
        }

        public InjectionProfile(string assemblyPath, string ns, string className, string methodName, bool stealthMode)
        {
            Name = System.IO.Path.GetFileNameWithoutExtension(assemblyPath) ?? "Unnamed";
            AssemblyPath = assemblyPath;
            Namespace = ns;
            ClassName = className;
            MethodName = methodName;
            EjectNamespace = ns;
            EjectClassName = className;
            EjectMethodName = methodName == "Load" ? "Unload" : methodName;
            UseStealthMode = stealthMode;
            LastUsed = DateTime.Now;
        }

        public override string ToString()
        {
            return $"{Name} - {ClassName}.{MethodName}";
        }
    }
}