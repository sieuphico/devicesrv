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
                // Toggle sort direction: Usually null initially, so we default to Ascending first click.
                var isDescending = e.Column.SortDirection == DataGridSortDirection.Ascending;
                
                // Clear other columns' sort indicators
                foreach (var column in grid.Columns)
                {
                    column.SortDirection = null;
                }

                e.Column.SortDirection = isDescending ? DataGridSortDirection.Descending : DataGridSortDirection.Ascending;

                var columnName = e.Column.Tag?.ToString() ?? "Id";
                System.Diagnostics.Debug.WriteLine($"[SORT LOG] UI Grid Clicked: Header={e.Column.Header}, ColumnTag={columnName}, Descending={isDescending}");
                
                if (grid == ModelGrid)
                {
                    ViewModel.UpdateModelSort(columnName, isDescending);
                }
                else if (grid == DeviceGrid)
                {
                    ViewModel.UpdateDeviceSort(columnName, isDescending);
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
            if (ViewModel.CanGoModelPrev)
            {
                ViewModel.ModelCurrentPage--;
            }
        }

        private void ModelNextPage_Click(object sender, RoutedEventArgs e)
        {
            if (ViewModel.CanGoModelNext)
            {
                ViewModel.ModelCurrentPage++;
            }
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

        private void FilterGrid_Loaded(object sender, RoutedEventArgs e)
        {
            if (sender is Grid grid)
            {
                // Clear any manual widths if set previously
                grid.Width = double.NaN; 
            
                Microsoft.UI.Xaml.DependencyObject parent = Microsoft.UI.Xaml.Media.VisualTreeHelper.GetParent(grid);
                while (parent != null && !(parent is CommunityToolkit.WinUI.UI.Controls.Primitives.DataGridColumnHeader))
                {
                    parent = Microsoft.UI.Xaml.Media.VisualTreeHelper.GetParent(parent);
                }
                
                if (parent is CommunityToolkit.WinUI.UI.Controls.Primitives.DataGridColumnHeader header)
                {
                    // Unbind ContentPresenter from Auto Column constraints and force Stretch
                    var presenter = FindVisualChildByType<Microsoft.UI.Xaml.Controls.ContentPresenter>(header);
                    if (presenter != null)
                    {
                        Microsoft.UI.Xaml.Controls.Grid.SetColumnSpan(presenter, 2);
                        presenter.HorizontalAlignment = Microsoft.UI.Xaml.HorizontalAlignment.Stretch;
                        presenter.HorizontalContentAlignment = Microsoft.UI.Xaml.HorizontalAlignment.Stretch;
                    }

                    // Reposition SortIcon perfectly inside Row 0
                    var sortIcon = FindVisualChildByName<Microsoft.UI.Xaml.Controls.FontIcon>(header, "SortIcon");
                    if (sortIcon != null)
                    {
                        sortIcon.VerticalAlignment = Microsoft.UI.Xaml.VerticalAlignment.Top;
                        sortIcon.HorizontalAlignment = Microsoft.UI.Xaml.HorizontalAlignment.Right;
                        sortIcon.Margin = new Microsoft.UI.Xaml.Thickness(0, 12, 12, 0); 
                    }
                }
            }
        }

        private T? FindVisualChildByName<T>(Microsoft.UI.Xaml.DependencyObject parent, string name) where T : Microsoft.UI.Xaml.DependencyObject
        {
            for (int i = 0; i < Microsoft.UI.Xaml.Media.VisualTreeHelper.GetChildrenCount(parent); i++)
            {
                var child = Microsoft.UI.Xaml.Media.VisualTreeHelper.GetChild(parent, i);
                if (child is T typedChild && (child as Microsoft.UI.Xaml.FrameworkElement)?.Name == name)
                {
                    return typedChild;
                }
                var result = FindVisualChildByName<T>(child, name);
                if (result != null) return result;
            }
            return null;
        }

        private T? FindVisualChildByType<T>(Microsoft.UI.Xaml.DependencyObject parent) where T : Microsoft.UI.Xaml.DependencyObject
        {
            for (int i = 0; i < Microsoft.UI.Xaml.Media.VisualTreeHelper.GetChildrenCount(parent); i++)
            {
                var child = Microsoft.UI.Xaml.Media.VisualTreeHelper.GetChild(parent, i);
                if (child is T typedChild)
                {
                    return typedChild;
                }
                var result = FindVisualChildByType<T>(child);
                if (result != null) return result;
            }
            return null;
        }
    }
}
