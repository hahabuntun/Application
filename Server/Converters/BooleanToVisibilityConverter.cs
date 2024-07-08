

using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace Server
{
    public class BooleanToVisibilityConverter: IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool && (bool)value)
            {
                return Brushes.Green; // Зеленый цвет, если true
            }
            else
            {
                return Brushes.Red;   // Красный цвет, если false
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}