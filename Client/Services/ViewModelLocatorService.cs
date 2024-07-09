using Microsoft.Extensions.DependencyInjection;
using Client.ViewModels;

namespace Client.Services
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
        

        public ClientViewModel ClientViewModel => _provider.GetRequiredService<ClientViewModel>();
        public AllMessagesViewModel AllMessagesViewModel => _provider.GetRequiredService<AllMessagesViewModel>();
    }
}
