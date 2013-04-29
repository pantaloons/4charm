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

            //TODO: Brushes
            if (cc == APIPost.CapCodes.admin || cc == APIPost.CapCodes.admin_highlight) return new SolidColorBrush((Color)App.Current.Resources["AdminColor"]);
            else if (cc == APIPost.CapCodes.mod || cc == APIPost.CapCodes.quiet_mod) return new SolidColorBrush((Color)App.Current.Resources["ModColor"]);
            else if (cc == APIPost.CapCodes.developer) return new SolidColorBrush((Color)App.Current.Resources["DeveloperColor"]);
            else return new SolidColorBrush((Color)App.Current.Resources["NormalColor"]);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}
