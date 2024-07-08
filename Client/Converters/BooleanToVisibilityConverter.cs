﻿
using System.Globalization;
using System.Windows.Data;
using System.Windows;

namespace Client
{
    public class BooleanToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool && (bool)value)
            {
                return Visibility.Visible; // Видимый, если true
            }
            else
            {
                return Visibility.Collapsed; // Скрытый, если false
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is Visibility)
            {
                return (Visibility)value == Visibility.Visible;
            }
            else
            {
                return false;
            }
        }
    }
}
