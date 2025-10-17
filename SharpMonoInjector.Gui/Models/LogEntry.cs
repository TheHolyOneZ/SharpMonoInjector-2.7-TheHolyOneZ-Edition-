using System;

namespace SharpMonoInjector.Gui.Models
{
    public enum LogLevel
    {
        Info,
        Warning,
        Error,
        Success
    }

    public class LogEntry
    {
        public DateTime Timestamp { get; set; }
        public LogLevel Level { get; set; }
        public string Message { get; set; }
        public string Source { get; set; }

        public LogEntry(LogLevel level, string message, string source = "")
        {
            Timestamp = DateTime.Now;
            Level = level;
            Message = message;
            Source = source;
        }

        public string TimeString => Timestamp.ToString("HH:mm:ss");
        
        public string LevelString => Level.ToString().ToUpper();

        public string FullMessage => $"[{TimeString}] [{LevelString}] {(string.IsNullOrEmpty(Source) ? "" : $"[{Source}] ")}{Message}";
    }
}