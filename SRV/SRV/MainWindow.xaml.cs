using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using SRV.ViewModels;

namespace SRV
{
    public sealed partial class MainWindow : Window
    {
        public MainViewModel ViewModel { get; }

        public MainWindow()
        {
            ViewModel = App.Current.Services.GetRequiredService<MainViewModel>();
            this.InitializeComponent();
        }

        public static Microsoft.UI.Xaml.Media.Brush GetRowBrush(bool isBorrowed)
        {
            return isBorrowed 
                ? new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.Red) 
                : new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.Black);
        }

        private void BorrowButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.DataContext is ModelDto dto)
            {
                ViewModel.BorrowCommand.Execute(dto);
            }
        }

        private void Filter_KeyDown(object sender, KeyRoutedEventArgs e)
        {
            if (e.Key == Windows.System.VirtualKey.Enter)
            {
                SearchButton_Click(null, null);
            }
        }

        private void SearchButton_Click(object sender, RoutedEventArgs e)
        {
            ViewModel.LoadModelsCommand.Execute(null);
            ViewModel.LoadDevicesCommand.Execute(null);
        }

    }
}

