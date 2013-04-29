using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace _4charm.Converters
{
    public class BoolVisibleConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if(parameter is string) return (bool)value ? Visibility.Collapsed : Visibility.Visible;
            else return (bool)value ? Visibility.Visible : Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}
