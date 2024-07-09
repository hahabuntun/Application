
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace Server
{
    /// <summary>
    /// Преобразует строку в видимость. Если строка пустая или null, тогда видимости нет, иначе видимость есть
    /// </summary>
    public class StringToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            string strValue = value as string;
            if (!string.IsNullOrEmpty(strValue))
                return Visibility.Visible;
            else
                return Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
