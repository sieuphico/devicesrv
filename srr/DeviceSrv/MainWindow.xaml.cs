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

        private void DataGrid_Sorting(object? sender, DataGridColumnEventArgs e)
        {
            if (sender is DataGrid grid)
            {
                var orderBy = e.Column.Tag?.ToString() ?? "Id";
                var isDescending = e.Column.SortDirection == null || e.Column.SortDirection == DataGridSortDirection.Ascending;
                
                foreach (var col in grid.Columns)
                {
                    if (col != e.Column) col.SortDirection = null;
                }
                
                e.Column.SortDirection = isDescending ? DataGridSortDirection.Descending : DataGridSortDirection.Ascending;
                
                if (grid == ModelGrid) ViewModel.UpdateModelSort(orderBy, isDescending);
                else if (grid == DeviceGrid) ViewModel.UpdateDeviceSort(orderBy, isDescending);
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

        private void Refresh_Click(object sender, RoutedEventArgs e) => ViewModel.RefreshAllAsync();

        private void Borrow_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.DataContext is Model model)
            {
                ViewModel.BorrowDevice(model, 1);
            }
        }

        private void PrevPage_Click(object sender, RoutedEventArgs e)
        {
            if (ViewModel.CanGoPrev) ViewModel.CurrentPage--;
        }

        private void NextPage_Click(object sender, RoutedEventArgs e)
        {
            if (ViewModel.CanGoNext) ViewModel.CurrentPage++;
        }

        private void FirstPage_Click(object sender, RoutedEventArgs e)
        {
            ViewModel.CurrentPage = 1;
        }

        private void LastPage_Click(object sender, RoutedEventArgs e)
        {
            ViewModel.CurrentPage = ViewModel.TotalPages;
        }

        private void ModelPrevPage_Click(object sender, RoutedEventArgs e)
        {
            if (ViewModel.CanGoModelPrev) ViewModel.ModelCurrentPage--;
        }

        private void ModelNextPage_Click(object sender, RoutedEventArgs e)
        {
            if (ViewModel.CanGoModelNext) ViewModel.ModelCurrentPage++;
        }

        private void ModelFirstPage_Click(object sender, RoutedEventArgs e)
        {
            ViewModel.ModelCurrentPage = 1;
        }

        private void ModelLastPage_Click(object sender, RoutedEventArgs e)
        {
            ViewModel.ModelCurrentPage = ViewModel.ModelTotalPages;
        }

        private void ToggleDevice_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.DataContext is Device device)
            {
                ViewModel.ToggleDeviceBorrowStatus(device);
            }
        }
    }
}
