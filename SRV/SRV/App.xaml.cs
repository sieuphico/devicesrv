using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using SRV.Services;
using SRV.ViewModels;
using System;

namespace SRV
{
    public partial class App : Application
    {
        private Window? _window;
        public IServiceProvider Services { get; }

        public App()
        {
            InitializeComponent();
            
            var services = new ServiceCollection();
            
            // Register Services tuân thủ Dependency Inversion
            services.AddSingleton<IDeviceService, DeviceService>();
            
            // Register ViewModels
            services.AddTransient<MainViewModel>();
            
            Services = services.BuildServiceProvider();
        }

        public new static App Current => (App)Application.Current;

        protected override void OnLaunched(Microsoft.UI.Xaml.LaunchActivatedEventArgs args)
        {
            _window = new MainWindow();
            _window.Activate();
        }
    }
}
