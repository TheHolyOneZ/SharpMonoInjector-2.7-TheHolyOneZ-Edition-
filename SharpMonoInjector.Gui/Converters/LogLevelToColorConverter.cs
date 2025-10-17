using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;
using SharpMonoInjector.Gui.Models;

namespace SharpMonoInjector.Gui.Converters
{
    public class LogLevelToColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is LogLevel level)
            {
                switch (level)
                {
                    case LogLevel.Info:
                        return new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FF00A8E8"));
                    case LogLevel.Warning:
                        return new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFFFA500"));
                    case LogLevel.Error:
                        return new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFFF4444"));
                    case LogLevel.Success:
                        return new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FF00E676"));
                    default:
                        return new SolidColorBrush(Colors.White);
                }
            }
            return new SolidColorBrush(Colors.White);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}