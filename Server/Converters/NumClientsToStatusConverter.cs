
using System.Globalization;
using System.Windows.Data;

namespace Server
{
    public class NumClientsToStatusConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is int count)
            {
                return $"Подключено {count}";
            }
            return "Подключено 0"; // По умолчанию показываем "Подключено 0", если значение не целое число
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
