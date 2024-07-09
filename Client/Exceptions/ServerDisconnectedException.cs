namespace Client.Exceptions
{
    /// <summary>
    /// Ошибка при отключении сервера
    /// </summary>
    public class ServerDisconnectedException : Exception
    {
        public ServerDisconnectedException()
            :base(){}
    }
}
