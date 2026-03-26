using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using DeviceSrv.Models;
using DeviceSrv.Services;
using System.Linq;

namespace DeviceSrv.ViewModels
{
    public class MainViewModel : INotifyPropertyChanged
    {
        private readonly DeviceService _deviceService = new();
        private DispatcherTimer _syncTimer;

        public ObservableCollection<Model> Models { get; } = new();
        public ObservableCollection<Device> Devices { get; } = new();

        public ObservableCollection<string> ModelCategories { get; } = new();

        // Error Handling
        private string _errorMessage = "";
        public string ErrorMessage { get => _errorMessage; set { _errorMessage = value; OnPropertyChanged(); OnPropertyChanged(nameof(HasError)); } }
        public bool HasError => !string.IsNullOrEmpty(ErrorMessage);

        // Sorting
        private string _modelOrderBy = "Name";
        public string ModelOrderBy { get => _modelOrderBy; set { _modelOrderBy = value; OnPropertyChanged(); } }
        private bool _modelIsDescending = false;
        public bool ModelIsDescending { get => _modelIsDescending; set { _modelIsDescending = value; OnPropertyChanged(); } }

        private string _deviceOrderBy = "Id";
        public string DeviceOrderBy { get => _deviceOrderBy; set { _deviceOrderBy = value; OnPropertyChanged(); } }
        private bool _deviceIsDescending = false;
        public bool DeviceIsDescending { get => _deviceIsDescending; set { _deviceIsDescending = value; OnPropertyChanged(); } }

        public void UpdateModelSort(string orderBy, bool isDescending)
        {
            System.Diagnostics.Debug.WriteLine($"[SORT LOG] Requesting Model Sort: Column={orderBy}, Descending={isDescending}");
            _modelOrderBy = orderBy;
            _modelIsDescending = isDescending;
            OnPropertyChanged(nameof(ModelOrderBy));
            OnPropertyChanged(nameof(ModelIsDescending));
            _ = LoadModelsAsync();
        }

        public void UpdateDeviceSort(string orderBy, bool isDescending)
        {
            System.Diagnostics.Debug.WriteLine($"[SORT LOG] Requesting Device Sort: Column={orderBy}, Descending={isDescending}");
            _deviceOrderBy = orderBy;
            _deviceIsDescending = isDescending;
            OnPropertyChanged(nameof(DeviceOrderBy));
            OnPropertyChanged(nameof(DeviceIsDescending));
            _ = LoadDevicesAsync();
        }

        // Model Filters
        private string _modelNameFilter = "";
        public string ModelNameFilter { get => _modelNameFilter; set { _modelNameFilter = value; OnPropertyChanged(); LoadModelsAsync(); } }
        private string _modelManufacturerFilter = "";
        public string ModelManufacturerFilter { get => _modelManufacturerFilter; set { _modelManufacturerFilter = value; OnPropertyChanged(); LoadModelsAsync(); } }
        
        private bool _isUpdatingCategories = false;
        private string _modelCategoryFilter = "All";
        public string ModelCategoryFilter 
        { 
            get => _modelCategoryFilter; 
            set 
            { 
                if (_modelCategoryFilter != value)
                {
                    _modelCategoryFilter = value ?? "All"; 
                    OnPropertyChanged(); 
                    if (!_isUpdatingCategories) 
                    {
                        LoadModelsAsync(); 
                    }
                }
            } 
        }

        // Device Filters
        private string _deviceNameFilter = "";
        public string DeviceNameFilter { get => _deviceNameFilter; set { _deviceNameFilter = value; OnPropertyChanged(); LoadDevicesAsync(); } }
        private string _deviceImeiFilter = "";
        public string DeviceImeiFilter { get => _deviceImeiFilter; set { _deviceImeiFilter = value; OnPropertyChanged(); LoadDevicesAsync(); } }
        private string _deviceSnFilter = "";
        public string DeviceSnFilter { get => _deviceSnFilter; set { _deviceSnFilter = value; OnPropertyChanged(); LoadDevicesAsync(); } }

        // Pagination
        private int _pageSize = 10;
        public int PageSize { get => _pageSize; set { _pageSize = value; OnPropertyChanged(); LoadDevicesAsync(); } }
        private int _totalCount;
        public int TotalCount { get => _totalCount; set { _totalCount = value; OnPropertyChanged(); OnPropertyChanged(nameof(TotalPages)); OnPropertyChanged(nameof(CanGoPrev)); OnPropertyChanged(nameof(CanGoNext)); } }
        public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);
        public bool CanGoPrev => CurrentPage > 1;
        public bool CanGoNext => CurrentPage < TotalPages;
        private int _currentPage = 1;
        public int CurrentPage { get => _currentPage; set { _currentPage = value; OnPropertyChanged(); OnPropertyChanged(nameof(CanGoPrev)); OnPropertyChanged(nameof(CanGoNext)); LoadDevicesAsync(); } }

        public MainViewModel()
        {
            InitializeAsync();
            _syncTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(5) };
            _syncTimer.Tick += (s, e) => { RefreshAllAsync(); };
            _syncTimer.Start();
        }

        private async void InitializeAsync()
        {
            try
            {
                ErrorMessage = "";
                await LoadModelsAsync();
                await LoadDevicesAsync();
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Critical error during initialization: {ex.Message}";
            }
        }

        public async Task LoadModelsAsync()
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"[SORT LOG] Executing Model DB Query: ORDER BY {ModelOrderBy} {(ModelIsDescending ? "DESC" : "ASC")}");
                var models = await _deviceService.GetModelsAsync(ModelOrderBy, ModelIsDescending);
                System.Diagnostics.Debug.WriteLine($"[SORT LOG] Loaded {models.Count()} Models from DB.");
                
                // Extract unique categories (once or merge carefully to avoid resetting selection loop)
                var uniqueCategories = models.Select(m => m.Category).Where(c => !string.IsNullOrEmpty(c)).Distinct().OrderBy(c => c).ToList();
                uniqueCategories.Insert(0, "All");
                
                if (!ModelCategories.SequenceEqual(uniqueCategories))
                {
                    _isUpdatingCategories = true;
                    var oldFilter = ModelCategoryFilter;
                    
                    ModelCategories.Clear();
                    foreach (var cat in uniqueCategories)
                    {
                        ModelCategories.Add(cat);
                    }
                    
                    if (oldFilter != null && ModelCategories.Contains(oldFilter))
                    {
                        ModelCategoryFilter = oldFilter;
                    }
                    else
                    {
                        ModelCategoryFilter = "All";
                    }
                    _isUpdatingCategories = false;
                }

                var filtered = models.Where(m => 
                    (string.IsNullOrEmpty(ModelNameFilter) || m.Name.Contains(ModelNameFilter, StringComparison.OrdinalIgnoreCase)) &&
                    (string.IsNullOrEmpty(ModelManufacturerFilter) || m.Manufacturer.Contains(ModelManufacturerFilter, StringComparison.OrdinalIgnoreCase)) &&
                    (ModelCategoryFilter == "All" || m.Category == ModelCategoryFilter)
                );

                Models.Clear();
                foreach (var m in filtered) Models.Add(m);
                ErrorMessage = "";
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Error loading models: {ex.Message}";
            }
        }

        public async Task LoadDevicesAsync()
        {
            try
            {
                var combinedFilter = $"{DeviceNameFilter} {DeviceImeiFilter} {DeviceSnFilter}".Trim();
                var (devices, total) = await _deviceService.GetDevicesPagedAsync(CurrentPage, PageSize, DeviceOrderBy, DeviceIsDescending, combinedFilter);
                
                Devices.Clear();
                foreach (var d in devices) Devices.Add(d);
                TotalCount = total;
                ErrorMessage = "";
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Error loading devices: {ex.Message}";
            }
        }

        public async void RefreshAllAsync()
        {
            await LoadModelsAsync();
            await LoadDevicesAsync();
        }

        public async void BorrowDevice(Model model, int quantity)
        {
            try
            {
                if (model.Available >= quantity)
                {
                    var success = await _deviceService.BorrowDevicesAsync(model.Id, quantity);
                    if (success)
                    {
                        RefreshAllAsync();
                    }
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Error borrowing device: {ex.Message}";
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? name = null) => 
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
