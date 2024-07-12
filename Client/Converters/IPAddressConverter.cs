

using System.Globalization;
using System.Net;
using System.Windows.Data;

namespace Client
{
    public class IPAddressConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string ipAddressString)
            {
                // Remove any "::fffff:" prefix
                ipAddressString = ipAddressString.Replace("f", "");
                ipAddressString = ipAddressString.Replace(":", "");
                return ipAddressString;
            }

            return value; // Return the original value if it's not a string
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            // Not needed for this scenario
            throw new NotImplementedException();
        }
    }
}
