using _4charm.Models.API;
using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace _4charm.Converters
{
    public class CapCodeColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            APIPost.CapCodes cc = (APIPost.CapCodes)value;

            if (cc == APIPost.CapCodes.admin || cc == APIPost.CapCodes.admin_highlight) return App.Current.Resources["AdminBrush"] as SolidColorBrush;
            else if (cc == APIPost.CapCodes.mod || cc == APIPost.CapCodes.quiet_mod) return App.Current.Resources["ModBrush"] as SolidColorBrush;
            else if (cc == APIPost.CapCodes.developer) return App.Current.Resources["DeveloperBrush"] as SolidColorBrush;
            else return App.Current.Resources["NormalBrush"] as SolidColorBrush;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}
