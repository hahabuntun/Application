
using System.Globalization;
using System.Net.Sockets;
using System.Net;

using System.Windows.Data;

namespace Server
{
    public class TcpClientToAddressPortConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is TcpClient client)
            {
                if (client == null)
                {
                    return null;
                }
                IPEndPoint? clientEndPoint = client.Client.RemoteEndPoint as IPEndPoint;
                var ClientAddress = clientEndPoint.Address.ToString();
                var ClientPort = clientEndPoint.Port;
                return $"{ClientAddress}:{ClientPort}";
            }
            return null;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            // Not needed for ComboBox in this case
            throw new NotImplementedException();
        }
    }
}
