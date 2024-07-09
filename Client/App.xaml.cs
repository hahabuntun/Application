
using Client.Services;
using Client.Services.Client;
using Client.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Serilog;
using System.Windows;

namespace Client
{
    public partial class App : Application
    {
        private readonly IServiceCollection services = new ServiceCollection();
        private readonly IServiceProvider _serviceProvider;
        public App()
        {
            Log.Logger = new LoggerConfiguration()
                .WriteTo.File("logs/log.txt", rollingInterval: RollingInterval.Day)
                .CreateLogger();

            services.AddLogging(config =>
            {
                config.ClearProviders();
                config.AddSerilog();
            });
            services.AddSingleton<ClientViewModel>();
            services.AddSingleton<ITCPClientService, TCPClientService>();
            services.AddSingleton<AllMessagesViewModel>();

            services.AddSingleton<ViewModelLocatorService>();
            services.AddSingleton<WindowMapperService>();
            services.AddSingleton<IWindowManagerService, WindowManagerService>();

            _serviceProvider = services.BuildServiceProvider();
        }
        protected override void OnStartup(StartupEventArgs e)
        {
            var windowManager = _serviceProvider.GetRequiredService<IWindowManagerService>();
            windowManager.ShowWindow(_serviceProvider.GetRequiredService<ClientViewModel>());
            base.OnStartup(e);
        }
    }

}
