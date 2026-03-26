using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System.IO;
using System;

namespace DeviceSrv
{
    public partial class App : Application
    {
        private Window? m_window;

        public App()
        {
            this.InitializeComponent();
            this.UnhandledException += App_UnhandledException;
            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
        }

        private void App_UnhandledException(object sender, Microsoft.UI.Xaml.UnhandledExceptionEventArgs e)
        {
            LogCrash(e.Exception);
        }

        private void CurrentDomain_UnhandledException(object sender, System.UnhandledExceptionEventArgs e)
        {
            if (e.ExceptionObject is Exception ex)
            {
                LogCrash(ex);
            }
        }

        private void LogCrash(Exception ex)
        {
            try
            {
                File.WriteAllText("crash.log", $"[{DateTime.Now}] CRASH: {ex.Message}\nStackTrace:\n{ex.StackTrace}\nInnerException: {ex.InnerException?.Message}\nInnerStackTrace:\n{ex.InnerException?.StackTrace}");
            }
            catch { }
        }

        protected override void OnLaunched(LaunchActivatedEventArgs e)
        {
            try 
            {
                m_window = new MainWindow();
                m_window.Activate();
            }
            catch (Exception ex)
            {
                LogCrash(ex);
                throw;
            }
        }
    }
}
