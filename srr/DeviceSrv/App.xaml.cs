using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace DeviceSrv
{
    public partial class App : Application
    {
        private Window? m_window;

        public App()
        {
            this.InitializeComponent();
        }

        protected override void OnLaunched(LaunchActivatedEventArgs e)
        {
            m_window = new MainWindow();
            m_window.Activate();
        }
    }
}
