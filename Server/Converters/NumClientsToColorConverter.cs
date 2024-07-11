

using System.Collections.ObjectModel;
using System.Globalization;
using System.Net.Sockets;
using System.Windows.Data;
using System.Windows.Media;

namespace Server
{
    public class NumClientsToColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is int count)
            {
                // Если количество клиентов больше 0, возвращаем зеленый цвет, иначе красный
                return count > 0 ? Brushes.Green : Brushes.Red;
            }
            return Brushes.Green; // По умолчанию красный цвет
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
