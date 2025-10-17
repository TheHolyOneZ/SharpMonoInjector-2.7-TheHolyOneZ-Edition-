using System;
using System.Collections.Generic;
using System.Linq;

namespace SharpMonoInjector.Gui.Models
{
    [Serializable]
    public class AppSettings
    {
        public List<string> RecentAssemblies { get; set; } = new List<string>();
        public List<string> RecentProcesses { get; set; } = new List<string>();
        public List<InjectionProfile> RecentProfiles { get; set; } = new List<InjectionProfile>();
        public InjectionProfile LastProfile { get; set; }
        public bool AutoLoadLastProfile { get; set; } = true;
        public bool StealthModeDefault { get; set; } = false;
        public int MaxRecentItems { get; set; } = 10;
        public string LastAssemblyPath { get; set; }
        public string LastProcessName { get; set; }
        public WindowSettings WindowSettings { get; set; } = new WindowSettings();
        public ProcessMonitorSettings MonitorSettings { get; set; } = new ProcessMonitorSettings();

        public void AddRecentAssembly(string path)
        {
            if (string.IsNullOrEmpty(path)) return;
            
            RecentAssemblies.Remove(path);
            RecentAssemblies.Insert(0, path);
            
            if (RecentAssemblies.Count > MaxRecentItems)
                RecentAssemblies = RecentAssemblies.Take(MaxRecentItems).ToList();
        }

        public void AddRecentProcess(string processName)
        {
            if (string.IsNullOrEmpty(processName)) return;
            
            RecentProcesses.Remove(processName);
            RecentProcesses.Insert(0, processName);
            
            if (RecentProcesses.Count > MaxRecentItems)
                RecentProcesses = RecentProcesses.Take(MaxRecentItems).ToList();
        }

        public void AddRecentProfile(InjectionProfile profile)
        {
            if (profile == null) return;
            
            var existing = RecentProfiles.FirstOrDefault(p => p.Name == profile.Name);
            if (existing != null)
                RecentProfiles.Remove(existing);
            
            RecentProfiles.Insert(0, profile);
            
            if (RecentProfiles.Count > MaxRecentItems)
                RecentProfiles = RecentProfiles.Take(MaxRecentItems).ToList();
        }
    }

    [Serializable]
    public class WindowSettings
    {
        public double Left { get; set; } = 100;
        public double Top { get; set; } = 100;
        public double Width { get; set; } = 900;
        public double Height { get; set; } = 500;
    }
}