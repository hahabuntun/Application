
namespace Server.Exceptions
{
    /// <summary>
    /// Ошибка при отключении клиента
    /// </summary>
    public class ClientDisconnectedException : Exception
    {
        public ClientDisconnectedException()
            : base() { }
    }
}
