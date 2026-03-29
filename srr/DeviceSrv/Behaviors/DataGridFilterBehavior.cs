using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
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

                    var sortIcon = FindVisualChildByName<FontIcon>(header, "SortIcon");
                    if (sortIcon != null)
                    {
                        sortIcon.VerticalAlignment = VerticalAlignment.Top;
                        sortIcon.HorizontalAlignment = HorizontalAlignment.Right;
                        sortIcon.Margin = new Thickness(0, 12, 12, 0); 
                        sortIcon.Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.White);
                        Canvas.SetZIndex(sortIcon, 999); 
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
        // EnableComboBoxFilter Property: Attached to ComboBox to handle ItemsSource and SelectionChanged
        // -------------------------------------------------------------------------
        public static readonly DependencyProperty EnableComboBoxFilterProperty =
            DependencyProperty.RegisterAttached(
                "EnableComboBoxFilter", typeof(bool), typeof(DataGridFilterBehavior),
                new PropertyMetadata(false, OnEnableComboBoxFilterChanged));

        public static bool GetEnableComboBoxFilter(DependencyObject obj) => (bool)obj.GetValue(EnableComboBoxFilterProperty);
        public static void SetEnableComboBoxFilter(DependencyObject obj, bool value) => obj.SetValue(EnableComboBoxFilterProperty, value);

        private static void OnEnableComboBoxFilterChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is ComboBox box)
            {
                if ((bool)e.NewValue)
                {
                    box.Loaded += ComboBox_Loaded;
                    box.SelectionChanged += ComboBox_SelectionChanged;
                }
                else
                {
                    box.Loaded -= ComboBox_Loaded;
                    box.SelectionChanged -= ComboBox_SelectionChanged;
                }
            }
        }

        private static void ComboBox_Loaded(object sender, RoutedEventArgs e)
        {
            if (sender is ComboBox box)
            {
                var (grid, tag) = GetGridAndTag(box);
                if (grid != null && !string.IsNullOrEmpty(tag))
                {
                    var filterKey = $"{grid.Name}_{tag}";
                    if (grid.DataContext is FilterableViewModel vm)
                    {
                        if (!vm.FilterOptions.TryGetValue(filterKey, out var options))
                        {
                            options = new System.Collections.ObjectModel.ObservableCollection<string>();
                            vm.FilterOptions[filterKey] = options;
                        }
                        box.ItemsSource = options;

                        if (vm.FilterHeaders.TryGetValue(filterKey, out var currentVal) && !string.IsNullOrEmpty(currentVal))
                        {
                            box.SelectedItem = currentVal;
                        }
                        else
                        {
                            box.SelectedItem = "All";
                        }
                    }
                }
            }
        }

        private static void ComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (sender is ComboBox box && box.IsLoaded) // Ensure it doesn't trigger unexpectedly during initialization
            {
                var (grid, tag) = GetGridAndTag(box);
                if (grid != null && !string.IsNullOrEmpty(tag))
                {
                    var filterKey = $"{grid.Name}_{tag}";
                    var selectedVal = box.SelectedItem?.ToString() ?? "All";
                    
                    if (grid.DataContext is FilterableViewModel vm)
                    {
                        vm.UpdateFilter(filterKey, selectedVal);
                    }
                }
            }
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
