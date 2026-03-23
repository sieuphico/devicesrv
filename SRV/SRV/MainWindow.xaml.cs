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
                : (Microsoft.UI.Xaml.Media.Brush)Application.Current.Resources["TextFillColorPrimaryBrush"];
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

        private void DataGrid_Sorting(object sender, CommunityToolkit.WinUI.UI.Controls.DataGridColumnEventArgs e)
        {
            if (e.Column.Tag is string col)
            {
                // Check which DataGrid is sorting by looking at the parent or ItemsSource
                if (sender is CommunityToolkit.WinUI.UI.Controls.DataGrid dg)
                {
                    if (dg.ItemsSource == ViewModel.Models)
                    {
                        ViewModel.SortModelsCommand.Execute(col);
                    }
                    else if (dg.ItemsSource == ViewModel.Devices)
                    {
                        ViewModel.SortDevicesCommand.Execute(col);
                    }

                    // Reset sort markers on other columns
                    foreach (var column in dg.Columns)
                    {
                        if (column != e.Column)
                        {
                            column.SortDirection = null;
                        }
                    }

                    // Set sort marker on current column
                    bool isAsc = (dg.ItemsSource == ViewModel.Models) ? ViewModel.ModelSortAscending : ViewModel.DeviceSortAscending;
                    e.Column.SortDirection = isAsc 
                        ? CommunityToolkit.WinUI.UI.Controls.DataGridSortDirection.Ascending 
                        : CommunityToolkit.WinUI.UI.Controls.DataGridSortDirection.Descending;
                }
            }
        }
    }
}

