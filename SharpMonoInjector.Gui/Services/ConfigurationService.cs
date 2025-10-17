using System;
using System.IO;
using System.Xml.Serialization;
using SharpMonoInjector.Gui.Models;

namespace SharpMonoInjector.Gui.Services
{
    public class ConfigurationService
    {
        private static readonly string ConfigDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "SharpMonoInjector");
        private static readonly string ConfigFile = Path.Combine(ConfigDirectory, "settings.xml");
        private static ConfigurationService _instance;
        private static readonly object _lock = new object();

        public static ConfigurationService Instance
        {
            get
            {
                if (_instance == null)
                {
                    lock (_lock)
                    {
                        if (_instance == null)
                            _instance = new ConfigurationService();
                    }
                }
                return _instance;
            }
        }

        private ConfigurationService()
        {
            if (!Directory.Exists(ConfigDirectory))
                Directory.CreateDirectory(ConfigDirectory);
        }

        public AppSettings LoadSettings()
        {
            try
            {
                if (!File.Exists(ConfigFile))
                    return new AppSettings();

                using (FileStream fs = new FileStream(ConfigFile, FileMode.Open, FileAccess.Read))
                {
                    XmlSerializer serializer = new XmlSerializer(typeof(AppSettings));
                    return (AppSettings)serializer.Deserialize(fs);
                }
            }
            catch (Exception ex)
            {
                File.AppendAllText(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "DebugLog.txt"), 
                    $"[ConfigurationService] Failed to load settings: {ex.Message}\r\n");
                return new AppSettings();
            }
        }

        public void SaveSettings(AppSettings settings)
        {
            try
            {
                using (FileStream fs = new FileStream(ConfigFile, FileMode.Create, FileAccess.Write))
                {
                    XmlSerializer serializer = new XmlSerializer(typeof(AppSettings));
                    serializer.Serialize(fs, settings);
                }
            }
            catch (Exception ex)
            {
                File.AppendAllText(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "DebugLog.txt"), 
                    $"[ConfigurationService] Failed to save settings: {ex.Message}\r\n");
            }
        }

        public void SaveProfile(InjectionProfile profile)
        {
            var settings = LoadSettings();
            settings.AddRecentProfile(profile);
            settings.LastProfile = profile;
            SaveSettings(settings);
        }

        public InjectionProfile LoadLastProfile()
        {
            var settings = LoadSettings();
            return settings.AutoLoadLastProfile ? settings.LastProfile : null;
        }

        public void ClearRecentAssemblies()
        {
            var settings = LoadSettings();
            settings.RecentAssemblies.Clear();
            SaveSettings(settings);
        }

        public void ClearRecentProcesses()
        {
            var settings = LoadSettings();
            settings.RecentProcesses.Clear();
            SaveSettings(settings);
        }

        public void ClearAllRecents()
        {
            var settings = LoadSettings();
            settings.RecentAssemblies.Clear();
            settings.RecentProcesses.Clear();
            settings.RecentProfiles.Clear();
            SaveSettings(settings);
        }

        public void ResetSettings()
        {
            try
            {
                if (File.Exists(ConfigFile))
                    File.Delete(ConfigFile);
            }
            catch (Exception ex)
            {
                File.AppendAllText(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "DebugLog.txt"), 
                    $"[ConfigurationService] Failed to reset settings: {ex.Message}\r\n");
            }
        }
    }
}