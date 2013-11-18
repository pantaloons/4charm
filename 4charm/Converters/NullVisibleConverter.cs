using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace _4charm.Converters
{
    public class NullVisibleConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (parameter != null && (string)parameter == "flip") return value != null ? Visibility.Collapsed : Visibility.Visible;
            return value != null ? Visibility.Visible : Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}
