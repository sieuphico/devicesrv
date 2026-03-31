using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using DeviceSrv.ViewModels;
using DeviceSrv.Models;
using CommunityToolkit.WinUI.UI.Controls;

namespace DeviceSrv.Views
{
    public sealed partial class ModelListView : UserControl
    {
        public ModelListViewModel ViewModel => DataContext as ModelListViewModel;

        public ModelListView()
        {
            this.InitializeComponent();
            this.DataContextChanged += (s, e) => Bindings.Update();
        }

        private void Refresh_Click(object sender, RoutedEventArgs e)
        {
            _ = ViewModel?.LoadModelsAsync();
            _ = ViewModel?.LoadCategoriesAsync();
            _ = ViewModel?.LoadManufacturersAsync();
        }

        private void Borrow_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.DataContext is Model model)
            {
                ViewModel?.BorrowDevice(model, 1);
            }
        }

        private void ModelPrevPage_Click(object sender, RoutedEventArgs e)
        {
            if (ViewModel != null && ViewModel.CanGoModelPrev) ViewModel.ModelCurrentPage--;
        }

        private void ModelNextPage_Click(object sender, RoutedEventArgs e)
        {
            if (ViewModel != null && ViewModel.CanGoModelNext) ViewModel.ModelCurrentPage++;
        }

        private void ModelFirstPage_Click(object sender, RoutedEventArgs e)
        {
            if (ViewModel != null) ViewModel.ModelCurrentPage = 1;
        }

        private void ModelLastPage_Click(object sender, RoutedEventArgs e)
        {
            if (ViewModel != null) ViewModel.ModelCurrentPage = ViewModel.ModelTotalPages;
        }

        private void PageButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.DataContext is PaginationItem item && item.IsClickable)
            {
                if (ViewModel != null) ViewModel.ModelCurrentPage = item.Value;
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
                ViewModel.UpdateModelSort(orderBy, isDescending);
            }
        }
    }
}
