using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using DeviceSrv.ViewModels;
using DeviceSrv.Models;
using CommunityToolkit.WinUI.UI.Controls;

namespace DeviceSrv
{
    public partial class MainWindow : Window
    {
        public MainViewModel ViewModel { get; }

        public MainWindow()
        {
            this.InitializeComponent();
            ViewModel = new MainViewModel();
        }

        private void DataGrid_Sorting(object? sender, DataGridColumnEventArgs e)
        {
            if (sender is DataGrid grid)
            {
                // Toggle sort direction
                var isDescending = e.Column.SortDirection == null || e.Column.SortDirection == DataGridSortDirection.Ascending;
                
                // Clear other columns' sort indicators
                foreach (var column in grid.Columns)
                {
                    column.SortDirection = null;
                }

                e.Column.SortDirection = isDescending ? DataGridSortDirection.Descending : DataGridSortDirection.Ascending;

                // Update ViewModel and trigger server-side reload
                var columnName = e.Column.Tag?.ToString() ?? "Id";
                
                if (grid == ModelGrid)
                {
                    ViewModel.ModelOrderBy = columnName;
                    ViewModel.ModelIsDescending = isDescending;
                }
                else if (grid == DeviceGrid)
                {
                    ViewModel.DeviceOrderBy = columnName;
                    ViewModel.DeviceIsDescending = isDescending;
                }
            }
        }

        private void DeviceGrid_LoadingRow(object? sender, DataGridRowEventArgs e)
        {
            if (e.Row.DataContext is Device device && device.IsBorrowed)
            {
                e.Row.Background = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.Red);
            }
            else
            {
                e.Row.Background = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.Transparent);
            }
        }

        private void Refresh_Click(object sender, RoutedEventArgs e)
        {
            ViewModel.RefreshAllAsync();
        }

        private void Borrow_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.DataContext is Model model)
            {
                ViewModel.BorrowDevice(model, 1);
            }
        }

        private void PrevPage_Click(object sender, RoutedEventArgs e)
        {
            if (ViewModel.CanGoPrev)
            {
                ViewModel.CurrentPage--;
            }
        }

        private void NextPage_Click(object sender, RoutedEventArgs e)
        {
            if (ViewModel.CanGoNext)
            {
                ViewModel.CurrentPage++;
            }
        }
    }
}
