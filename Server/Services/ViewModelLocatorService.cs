using Microsoft.Extensions.DependencyInjection;
using Server.ViewModels;

namespace Server.Services
{
    /// <summary>
    /// Используется для получения нужной view модели
    /// </summary>
    public class ViewModelLocatorService
    {
        private readonly IServiceProvider _provider;


        public ViewModelLocatorService(IServiceProvider provider)
        {
            _provider = provider;
        }


        public AllConnectionsViewModel AllConnectionsViewModel => _provider.GetRequiredService<AllConnectionsViewModel>();
        public SingleConnectionViewModel SingleConnectionViewModel => _provider.GetRequiredService<SingleConnectionViewModel>();
        public AllMessagesViewModel AllMessagesViewModel => _provider.GetRequiredService<AllMessagesViewModel>();
    }
}
