using Microsoft.Extensions.DependencyInjection;
using Client.ViewModels;

namespace Client.Services
{
    public class ViewModelLocatorService
    {
        private readonly IServiceProvider _provider;

        public ViewModelLocatorService(IServiceProvider provider)
        {
            _provider = provider;
        }

        public ClientViewModel ClientViewModel => _provider.GetRequiredService<ClientViewModel>();
    }
}
