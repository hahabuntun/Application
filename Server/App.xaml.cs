using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Serilog;
using Server.Services;
using Server.Services.Server;
using Server.ViewModels;
using System.Windows;

namespace Server
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
            services.AddSingleton<AllConnectionsViewModel>();
            services.AddTransient<SingleConnectionViewModel>();
            services.AddTransient<AllMessagesViewModel>();
            services.AddTransient<ITCPServerService, TCPServerService>();

            services.AddSingleton<ViewModelLocatorService>();
            services.AddSingleton<WindowMapperService>();
            services.AddSingleton<IWindowManagerService, WindowManagerService>();

            _serviceProvider = services.BuildServiceProvider();
        }
        protected override void OnStartup(StartupEventArgs e)
        {
            var windowManager = _serviceProvider.GetRequiredService<IWindowManagerService>();
            windowManager.ShowWindow(_serviceProvider.GetRequiredService<AllConnectionsViewModel>());
            base.OnStartup(e);
        }
    }

}
