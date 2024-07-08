
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace Client
{
    public class BooleanToColorConverter : IValueConverter
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
