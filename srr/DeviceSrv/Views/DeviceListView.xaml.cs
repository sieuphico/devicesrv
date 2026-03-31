using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using DeviceSrv.ViewModels;
using DeviceSrv.Models;
using CommunityToolkit.WinUI.UI.Controls;

namespace DeviceSrv.Views
{
    public sealed partial class DeviceListView : UserControl
    {
        public DeviceListViewModel ViewModel => DataContext as DeviceListViewModel;

        public DeviceListView()
        {
            this.InitializeComponent();
            this.DataContextChanged += (s, e) => Bindings.Update();
        }

        private void ToggleDevice_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.DataContext is Device device)
            {
                ViewModel?.ToggleDeviceBorrowStatus(device);
            }
        }

        private void PrevPage_Click(object sender, RoutedEventArgs e)
        {
            if (ViewModel != null && ViewModel.CanGoPrev) ViewModel.CurrentPage--;
        }

        private void NextPage_Click(object sender, RoutedEventArgs e)
        {
            if (ViewModel != null && ViewModel.CanGoNext) ViewModel.CurrentPage++;
        }

        private void FirstPage_Click(object sender, RoutedEventArgs e)
        {
            if (ViewModel != null) ViewModel.CurrentPage = 1;
        }

        private void LastPage_Click(object sender, RoutedEventArgs e)
        {
            if (ViewModel != null) ViewModel.CurrentPage = ViewModel.TotalPages;
        }

        private void PageButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.DataContext is PaginationItem item && item.IsClickable)
            {
                if (ViewModel != null) ViewModel.CurrentPage = item.Value;
            }
        }

        private void DataGrid_Sorting(object? sender, DataGridColumnEventArgs e)
        {
            if (sender is DataGrid grid && ViewModel != null)
            {
                var orderBy = e.Column.Tag?.ToString() ?? "Id";
                var isDescending = e.Column.SortDirection == null || e.Column.SortDirection == DataGridSortDirection.Ascending;
                
                foreach (var col in grid.Columns)
                {
                    if (col != e.Column) col.SortDirection = null;
                }
                
                e.Column.SortDirection = isDescending ? DataGridSortDirection.Descending : DataGridSortDirection.Ascending;
                ViewModel.UpdateDeviceSort(orderBy, isDescending);
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
    }
}
