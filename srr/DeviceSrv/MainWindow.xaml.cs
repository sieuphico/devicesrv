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
            RootGrid.DataContext = ViewModel;

            // Bridge the ViewModel to the stable BindingProxy
            if (RootGrid.Resources["Proxy"] is BindingProxy proxy)
            {
                proxy.Data = ViewModel;
            }
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

        private void FilterGrid_Loaded(object sender, RoutedEventArgs e)
        {
            if (sender is Grid grid)
            {
                Microsoft.UI.Xaml.DependencyObject parent = Microsoft.UI.Xaml.Media.VisualTreeHelper.GetParent(grid);
                while (parent != null && !(parent is CommunityToolkit.WinUI.UI.Controls.Primitives.DataGridColumnHeader))
                {
                    parent = Microsoft.UI.Xaml.Media.VisualTreeHelper.GetParent(parent);
                }
                
                if (parent is CommunityToolkit.WinUI.UI.Controls.Primitives.DataGridColumnHeader header)
                {
                    // Update width based on header initially
                    UpdateGridWidth(grid, header);

                    // Update width dynamically when header resizes
                    header.SizeChanged += (s, args) => 
                    {
                        UpdateGridWidth(grid, header);
                    };
                }
            }
        }

        private void UpdateGridWidth(Grid innerGrid, FrameworkElement header)
        {
            // Reserve ~32px for the built-in sort arrow and default margins to ensure layout fits perfectly
            var targetWidth = header.ActualWidth - 32;
            if (targetWidth > 0)
            {
                innerGrid.Width = targetWidth;
            }
        }
    }
}
