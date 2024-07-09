

using System.Net;

namespace Server.Helpers
{
    public static class IPAddressHelper
    {
        /// <summary>
        /// Проверяет находится ли ip адресс сервера в необходимом диапазоне
        /// (сможет ли к нему подключиться клиент)
        /// Работает неправильно[в совокупности с вызывающей его функцией] :) 
        /// </summary>
        /// <param name="address"></param>
        /// <param name="start"></param>
        /// <param name="end"></param>
        /// <returns></returns>
        public static bool IsInRange(this IPAddress address, IPAddress start, IPAddress end)
        {
            byte[] addressBytes = address.GetAddressBytes();
            byte[] startBytes = start.GetAddressBytes();
            byte[] endBytes = end.GetAddressBytes();

            for (int i = 0; i < addressBytes.Length; i++)
            {
                if (addressBytes[i] < startBytes[i] || addressBytes[i] > endBytes[i])
                {
                    return false;
                }
            }

            return true;
        }
    }
}
