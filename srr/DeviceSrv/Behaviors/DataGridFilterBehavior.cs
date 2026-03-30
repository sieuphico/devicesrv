using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using CommunityToolkit.WinUI.UI.Controls.Primitives;
using System.Linq;
using DeviceSrv.ViewModels;

namespace DeviceSrv.Behaviors
{
    public static class DataGridFilterBehavior
    {
        // -------------------------------------------------------------------------
        // IsFilterHeader Property: Attached to Grid inside DataTemplate to handle Loaded
        // -------------------------------------------------------------------------
        public static readonly DependencyProperty IsFilterHeaderProperty =
            DependencyProperty.RegisterAttached(
                "IsFilterHeader", typeof(bool), typeof(DataGridFilterBehavior),
                new PropertyMetadata(false, OnIsFilterHeaderChanged));

        public static bool GetIsFilterHeader(DependencyObject obj) => (bool)obj.GetValue(IsFilterHeaderProperty);
        public static void SetIsFilterHeader(DependencyObject obj, bool value) => obj.SetValue(IsFilterHeaderProperty, value);

        private static void OnIsFilterHeaderChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is Grid grid)
            {
                if ((bool)e.NewValue)
                {
                    grid.Loaded += Grid_Loaded;
                }
                else
                {
                    grid.Loaded -= Grid_Loaded;
                }
            }
        }

        private static void Grid_Loaded(object sender, RoutedEventArgs e)
        {
            if (sender is FrameworkElement fe)
            {
                var parent = Microsoft.UI.Xaml.Media.VisualTreeHelper.GetParent(fe);
                while (parent != null && !(parent is DataGridColumnHeader))
                {
                    parent = Microsoft.UI.Xaml.Media.VisualTreeHelper.GetParent(parent);
                }
                
                if (parent is DataGridColumnHeader header)
                {
                    var presenter = FindVisualChildByType<ContentPresenter>(header);
                    if (presenter != null)
                    {
                        Grid.SetColumnSpan(presenter, 2);
                        presenter.HorizontalAlignment = HorizontalAlignment.Stretch;
                        presenter.HorizontalContentAlignment = HorizontalAlignment.Stretch;
                    }

                    // Hiding the native SortIcon to allow our custom one to shine
                    var sortIcon = FindVisualChildByName<FontIcon>(header, "SortIcon");
                    if (sortIcon != null)
                    {
                        sortIcon.Visibility = Visibility.Collapsed;
                    }
                }
            }
        }

        // -------------------------------------------------------------------------
        // EnableAutoFilter Property: Attached to AutoSuggestBox to handle TextChanged
        // -------------------------------------------------------------------------
        public static readonly DependencyProperty EnableAutoFilterProperty =
            DependencyProperty.RegisterAttached(
                "EnableAutoFilter", typeof(bool), typeof(DataGridFilterBehavior),
                new PropertyMetadata(false, OnEnableAutoFilterChanged));

        public static bool GetEnableAutoFilter(DependencyObject obj) => (bool)obj.GetValue(EnableAutoFilterProperty);
        public static void SetEnableAutoFilter(DependencyObject obj, bool value) => obj.SetValue(EnableAutoFilterProperty, value);

        private static void OnEnableAutoFilterChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is AutoSuggestBox box)
            {
                if ((bool)e.NewValue)
                {
                    box.TextChanged += Box_TextChanged;
                }
                else
                {
                    box.TextChanged -= Box_TextChanged;
                }
            }
        }

        private static void Box_TextChanged(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs args)
        {
            if (args.Reason == AutoSuggestionBoxTextChangeReason.UserInput)
            {
                var (grid, tag) = GetGridAndTag(sender);
                if (grid != null && !string.IsNullOrEmpty(tag))
                {
                    var filterKey = $"{grid.Name}_{tag}";
                    if (grid.DataContext is FilterableViewModel vm)
                    {
                        vm.UpdateFilter(filterKey, sender.Text);
                    }
                }
            }
        }

        // -------------------------------------------------------------------------
        // EnableCustomSearchFilter Property: Attached to Button to show a searchable Flyout
        // -------------------------------------------------------------------------
        public static readonly DependencyProperty EnableCustomSearchFilterProperty =
            DependencyProperty.RegisterAttached(
                "EnableCustomSearchFilter", typeof(bool), typeof(DataGridFilterBehavior),
                new PropertyMetadata(false, OnEnableCustomSearchFilterChanged));

        public static bool GetEnableCustomSearchFilter(DependencyObject obj) => (bool)obj.GetValue(EnableCustomSearchFilterProperty);
        public static void SetEnableCustomSearchFilter(DependencyObject obj, bool value) => obj.SetValue(EnableCustomSearchFilterProperty, value);

        private static void OnEnableCustomSearchFilterChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is Button btn)
            {
                if ((bool)e.NewValue)
                {
                    btn.Loaded += CustomFilterButton_Loaded;
                }
                else
                {
                    btn.Loaded -= CustomFilterButton_Loaded;
                }
            }
        }

        private static void CustomFilterButton_Loaded(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn)
            {
                // Set initial text based on ViewModel state
                UpdateSelectedValueText(btn);

                if (btn.Flyout is Flyout flyout)
                {
                    flyout.Opened -= CustomFilterFlyout_Opened;
                    flyout.Opened += CustomFilterFlyout_Opened;
                }
            }
        }

        private static void CustomFilterFlyout_Opened(object? sender, object e)
        {
            if (sender is Flyout flyout && flyout.Target is Button btn)
            {
                var searchBox = FindVisualChildByName<TextBox>(flyout.Content, "FilterSearchBox");
                var listView = FindVisualChildByName<ListView>(flyout.Content, "FilterListView");

                if (searchBox != null && listView != null)
                {
                    var (grid, tag) = GetGridAndTag(btn);
                    if (grid != null && !string.IsNullOrEmpty(tag))
                    {
                        var filterKey = $"{grid.Name}_{tag}";
                        if (grid.DataContext is FilterableViewModel vm && vm.FilterOptions.TryGetValue(filterKey, out var options))
                        {
                            listView.ItemsSource = options;
                            
                            // Highlight current selection
                            if (vm.FilterHeaders.TryGetValue(filterKey, out var current))
                            {
                                listView.SelectedItem = options.FirstOrDefault(o => o == current);
                            }

                            // Handle Search
                            searchBox.Text = ""; // Reset search on open
                            searchBox.TextChanged -= (s, arg) => FilterListViewItems(listView, options, searchBox.Text);
                            searchBox.TextChanged += (s, arg) => FilterListViewItems(listView, options, searchBox.Text);
                            
                            // Handle Selection
                            listView.SelectionChanged -= (s, arg) => 
                            {
                                if (listView.SelectedItem is string selected)
                                {
                                    vm.UpdateFilter(filterKey, selected);
                                    UpdateSelectedValueText(btn);
                                    flyout.Hide();
                                }
                            };
                            listView.SelectionChanged += (s, arg) => 
                            {
                                if (listView.SelectedItem is string selected)
                                {
                                    vm.UpdateFilter(filterKey, selected);
                                    UpdateSelectedValueText(btn);
                                    flyout.Hide();
                                }
                            };

                            searchBox.Focus(FocusState.Programmatic);
                        }
                    }
                }
            }
        }

        private static void FilterListViewItems(ListView listView, System.Collections.ObjectModel.ObservableCollection<string> allItems, string query)
        {
            if (string.IsNullOrWhiteSpace(query))
            {
                listView.ItemsSource = allItems;
            }
            else
            {
                listView.ItemsSource = allItems.Where(i => i.Contains(query, System.StringComparison.OrdinalIgnoreCase)).ToList();
            }
        }

        private static void UpdateSelectedValueText(Button btn)
        {
            var textBlock = FindVisualChildByName<TextBlock>(btn, "SelectedValueText");
            if (textBlock != null)
            {
                var (grid, tag) = GetGridAndTag(btn);
                if (grid != null && !string.IsNullOrEmpty(tag))
                {
                    var filterKey = $"{grid.Name}_{tag}";
                    if (grid.DataContext is FilterableViewModel vm)
                    {
                        if (vm.FilterHeaders.TryGetValue(filterKey, out var val) && !string.IsNullOrEmpty(val))
                        {
                            textBlock.Text = val;
                            textBlock.Foreground = (Brush)Application.Current.Resources["BlackBrush"];
                        }
                        else
                        {
                            textBlock.Text = "Filter...";
                            textBlock.Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.Gray);
                        }
                    }
                }
            }
        }

        // -------------------------------------------------------------------------
        // EnableAutoFilter Property: Attached to AutoSuggestBox to handle TextChanged
        public static readonly DependencyProperty IsCustomSortIconProperty =
            DependencyProperty.RegisterAttached(
                "IsCustomSortIcon", typeof(bool), typeof(DataGridFilterBehavior),
                new PropertyMetadata(false, OnIsCustomSortIconChanged));

        public static bool GetIsCustomSortIcon(DependencyObject obj) => (bool)obj.GetValue(IsCustomSortIconProperty);
        public static void SetIsCustomSortIcon(DependencyObject obj, bool value) => obj.SetValue(IsCustomSortIconProperty, value);

        private static void OnIsCustomSortIconChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is FontIcon icon && (bool)e.NewValue)
            {
                icon.Loaded += CustomSortIcon_Loaded;
                icon.Unloaded += CustomSortIcon_Unloaded;
            }
        }

        private static void CustomSortIcon_Loaded(object sender, RoutedEventArgs e)
        {
            if (sender is FontIcon icon)
            {
                var (grid, col) = GetGridAndColumn(icon);
                if (grid != null && col != null)
                {
                    UpdateCustomSortIcon(icon, col.SortDirection);

                    // Attach to DataGrid.Sorting to update ALL icons when sorting happens
                    grid.Sorting -= Grid_SortingForIcons; // Prevent duplicate hooks
                    grid.Sorting += Grid_SortingForIcons;
                }
            }
        }

        private static void CustomSortIcon_Unloaded(object sender, RoutedEventArgs e)
        {
            if (sender is FontIcon icon)
            {
                var (grid, _) = GetGridAndColumn(icon);
                if (grid != null) grid.Sorting -= Grid_SortingForIcons;
            }
        }

        private static void Grid_SortingForIcons(object? sender, CommunityToolkit.WinUI.UI.Controls.DataGridColumnEventArgs e)
        {
            if (sender is CommunityToolkit.WinUI.UI.Controls.DataGrid grid)
            {
                // Defer changing glyphs until after DataGrid updates the SortDirections in ViewModel/CodeBehind
                grid.DispatcherQueue.TryEnqueue(() =>
                {
                    var headersPresenter = FindVisualChildByType<DataGridColumnHeadersPresenter>(grid);
                    if (headersPresenter != null)
                    {
                        for (int i = 0; i < Microsoft.UI.Xaml.Media.VisualTreeHelper.GetChildrenCount(headersPresenter); i++)
                        {
                            var header = Microsoft.UI.Xaml.Media.VisualTreeHelper.GetChild(headersPresenter, i);
                            var icon = FindCustomSortIcon(header);
                            if (icon != null)
                            {
                                var (_, col) = GetGridAndColumn(icon);
                                if (col != null) UpdateCustomSortIcon(icon, col.SortDirection);
                            }
                        }
                    }
                });
            }
        }

        private static FontIcon? FindCustomSortIcon(DependencyObject parent)
        {
            for (int i = 0; i < Microsoft.UI.Xaml.Media.VisualTreeHelper.GetChildrenCount(parent); i++)
            {
                var child = Microsoft.UI.Xaml.Media.VisualTreeHelper.GetChild(parent, i);
                if (child is FontIcon icon && GetIsCustomSortIcon(icon)) return icon;
                var result = FindCustomSortIcon(child);
                if (result != null) return result;
            }
            return null;
        }

        private static void UpdateCustomSortIcon(FontIcon icon, CommunityToolkit.WinUI.UI.Controls.DataGridSortDirection? direction)
        {
            if (direction == null)
            {
                icon.Glyph = "\uE8CB"; // Biểu tượng Sort 2 chiều (Mũi tên lên/xuống chung)
            }
            else if (direction == CommunityToolkit.WinUI.UI.Controls.DataGridSortDirection.Ascending)
            {
                icon.Glyph = "\uE70E"; // Chevron Up (Biểu tượng chiều tăng ^)
            }
            else
            {
                icon.Glyph = "\uE70D"; // Chevron Down (Biểu tượng chiều giảm v)
            }
        }

        private static (CommunityToolkit.WinUI.UI.Controls.DataGrid? grid, CommunityToolkit.WinUI.UI.Controls.DataGridColumn? col) GetGridAndColumn(DependencyObject element)
        {
            var parent = Microsoft.UI.Xaml.Media.VisualTreeHelper.GetParent(element);
            while (parent != null && !(parent is DataGridColumnHeader))
            {
                parent = Microsoft.UI.Xaml.Media.VisualTreeHelper.GetParent(parent);
            }
            if (parent is DataGridColumnHeader header)
            {
                var gridParent = Microsoft.UI.Xaml.Media.VisualTreeHelper.GetParent(header);
                while (gridParent != null && !(gridParent is CommunityToolkit.WinUI.UI.Controls.DataGrid))
                {
                    gridParent = Microsoft.UI.Xaml.Media.VisualTreeHelper.GetParent(gridParent);
                }
                if (gridParent is CommunityToolkit.WinUI.UI.Controls.DataGrid grid)
                {
                    var headerContent = header.Content?.ToString();
                    var column = Enumerable.FirstOrDefault(grid.Columns, c => c.Header?.ToString() == headerContent);
                    return (grid, column);
                }
            }
            return (null, null);
        }

        // Helper to find Grid and Tag
        private static (CommunityToolkit.WinUI.UI.Controls.DataGrid? grid, string? tag) GetGridAndTag(DependencyObject element)
        {
            var parent = Microsoft.UI.Xaml.Media.VisualTreeHelper.GetParent(element);
            while (parent != null && !(parent is DataGridColumnHeader))
            {
                parent = Microsoft.UI.Xaml.Media.VisualTreeHelper.GetParent(parent);
            }
            
            if (parent is DataGridColumnHeader header)
            {
                var gridParent = Microsoft.UI.Xaml.Media.VisualTreeHelper.GetParent(header);
                while (gridParent != null && !(gridParent is CommunityToolkit.WinUI.UI.Controls.DataGrid))
                {
                    gridParent = Microsoft.UI.Xaml.Media.VisualTreeHelper.GetParent(gridParent);
                }
                
                if (gridParent is CommunityToolkit.WinUI.UI.Controls.DataGrid grid)
                {
                    var headerContent = header.Content?.ToString();
                    var column = Enumerable.FirstOrDefault(grid.Columns, c => c.Header?.ToString() == headerContent);
                    return (grid, column?.Tag?.ToString());
                }
            }
            return (null, null);
        }

        // -------------------------------------------------------------------------
        // EnableClearFilter Property: Attached to Button to clear all filters
        // -------------------------------------------------------------------------
        public static readonly DependencyProperty EnableClearFilterProperty =
            DependencyProperty.RegisterAttached(
                "EnableClearFilter", typeof(bool), typeof(DataGridFilterBehavior),
                new PropertyMetadata(false, OnEnableClearFilterChanged));

        public static bool GetEnableClearFilter(DependencyObject obj) => (bool)obj.GetValue(EnableClearFilterProperty);
        public static void SetEnableClearFilter(DependencyObject obj, bool value) => obj.SetValue(EnableClearFilterProperty, value);

        private static void OnEnableClearFilterChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is Button btn)
            {
                if ((bool)e.NewValue) btn.Click += Btn_ClearClick;
                else btn.Click -= Btn_ClearClick;
            }
        }

        private static void Btn_ClearClick(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn)
            {
                var parent = Microsoft.UI.Xaml.Media.VisualTreeHelper.GetParent(btn);
                while (parent != null && !(parent is CommunityToolkit.WinUI.UI.Controls.DataGrid))
                {
                    parent = Microsoft.UI.Xaml.Media.VisualTreeHelper.GetParent(parent);
                }

                if (parent is CommunityToolkit.WinUI.UI.Controls.DataGrid grid)
                {
                    if (grid.DataContext is FilterableViewModel vm)
                    {
                        vm.ClearFilters();
                        
                        // Clear all UI controls in the header
                        var headersPresenter = FindVisualChildByType<DataGridColumnHeadersPresenter>(grid);
                        if (headersPresenter != null)
                        {
                            ClearAllFilterUI(headersPresenter);
                        }
                    }
                }
            }
        }

        private static void ClearAllFilterUI(DependencyObject parent)
        {
            for (int i = 0; i < Microsoft.UI.Xaml.Media.VisualTreeHelper.GetChildrenCount(parent); i++)
            {
                var child = Microsoft.UI.Xaml.Media.VisualTreeHelper.GetChild(parent, i);
                if (child is AutoSuggestBox box) box.Text = "";
                else if (child is Button btn && GetEnableCustomSearchFilter(btn)) UpdateSelectedValueText(btn);
                else ClearAllFilterUI(child);
            }
        }

        // -------------------------------------------------------------------------
        // Visual Tree Helpers
        // -------------------------------------------------------------------------
        private static T? FindVisualChildByName<T>(DependencyObject parent, string name) where T : DependencyObject
        {
            for (int i = 0; i < Microsoft.UI.Xaml.Media.VisualTreeHelper.GetChildrenCount(parent); i++)
            {
                var child = Microsoft.UI.Xaml.Media.VisualTreeHelper.GetChild(parent, i);
                if (child is T typedChild && (child as FrameworkElement)?.Name == name) return typedChild;
                var result = FindVisualChildByName<T>(child, name);
                if (result != null) return result;
            }
            return null;
        }

        private static T? FindVisualChildByType<T>(DependencyObject parent) where T : DependencyObject
        {
            for (int i = 0; i < Microsoft.UI.Xaml.Media.VisualTreeHelper.GetChildrenCount(parent); i++)
            {
                var child = Microsoft.UI.Xaml.Media.VisualTreeHelper.GetChild(parent, i);
                if (child is T typedChild) return typedChild;
                var result = FindVisualChildByType<T>(child);
                if (result != null) return result;
            }
            return null;
        }
    }
}
