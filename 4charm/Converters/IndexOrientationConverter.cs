using Microsoft.Phone.Controls;
using System;
using System.Globalization;
using System.Windows.Data;

namespace _4charm.Converters
{
    public class IndexOrientationConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            switch ((SupportedPageOrientation)value)
            {
                case SupportedPageOrientation.PortraitOrLandscape:
                    return 0;
                case SupportedPageOrientation.Portrait:
                    return 1;
                case SupportedPageOrientation.Landscape:
                    return 2;
                default:
                    return 0;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if ((int)value == 0) return SupportedPageOrientation.PortraitOrLandscape;
            else if ((int)value == 1) return SupportedPageOrientation.Portrait;
            else return SupportedPageOrientation.Landscape;
        }
    }
}
