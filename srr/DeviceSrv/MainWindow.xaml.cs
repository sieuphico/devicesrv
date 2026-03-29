using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using DeviceSrv.ViewModels;
using DeviceSrv.Models;
using CommunityToolkit.WinUI.UI.Controls;

namespace DeviceSrv
{
    public partial class MainWindow : Window
    {
        public ViewModels.MainViewModel ViewModel { get; } = new();

        public MainWindow()
        {
            this.InitializeComponent();
            RootGrid.DataContext = this;

            // Bridge the ViewModel to the stable GlobalProxy defined in App.xaml
            if (Application.Current.Resources["GlobalProxy"] is BindingProxy proxy)
            {
                proxy.Data = ViewModel;
            }
        }

    }
}
