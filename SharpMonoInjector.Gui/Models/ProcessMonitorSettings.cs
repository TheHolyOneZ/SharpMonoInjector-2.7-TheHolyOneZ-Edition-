using System;
using System.Collections.Generic;

namespace SharpMonoInjector.Gui.Models
{
    [Serializable]
    public class ProcessMonitorSettings
    {
        public bool IsEnabled { get; set; }
        public List<WatchedProcess> WatchedProcesses { get; set; } = new List<WatchedProcess>();
        public bool AutoInjectOnDetection { get; set; }
        public bool ShowNotifications { get; set; } = true;
        public int MonitorIntervalMs { get; set; } = 2000;
        public string LastWatchedProcess { get; set; }
    }
}