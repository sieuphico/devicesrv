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
        private string _modelOrderBy = "Id";
        public string ModelOrderBy { get => _modelOrderBy; set { _modelOrderBy = value; OnPropertyChanged(); } }
        private bool _modelIsDescending = false;
        public bool ModelIsDescending { get => _modelIsDescending; set { _modelIsDescending = value; OnPropertyChanged(); } }

        private string _deviceOrderBy = "Id";
        public string DeviceOrderBy { get => _deviceOrderBy; set { _deviceOrderBy = value; OnPropertyChanged(); } }
        private bool _deviceIsDescending = false;
        public bool DeviceIsDescending { get => _deviceIsDescending; set { _deviceIsDescending = value; OnPropertyChanged(); } }

        public void UpdateModelSort(string orderBy, bool isDescending)
        {
            _modelOrderBy = orderBy;
            _modelIsDescending = isDescending;
            OnPropertyChanged(nameof(ModelOrderBy));
            OnPropertyChanged(nameof(ModelIsDescending));
            _modelCurrentPage = 1; // Reset to first page on sort
            OnPropertyChanged(nameof(ModelCurrentPage));
            _ = LoadModelsAsync();
        }

        public void UpdateDeviceSort(string orderBy, bool isDescending)
        {
            _deviceOrderBy = orderBy;
            _deviceIsDescending = isDescending;
            OnPropertyChanged(nameof(DeviceOrderBy));
            OnPropertyChanged(nameof(DeviceIsDescending));
            _currentPage = 1; // Reset to first page on sort
            OnPropertyChanged(nameof(CurrentPage));
            _ = LoadDevicesAsync();
        }

        // Model Filters
        private string _modelNameFilter = "";
        public string ModelNameFilter { get => _modelNameFilter; set { _modelNameFilter = value; OnPropertyChanged(); _modelCurrentPage = 1; LoadModelsAsync(); } }
        private string _modelManufacturerFilter = "";
        public string ModelManufacturerFilter { get => _modelManufacturerFilter; set { _modelManufacturerFilter = value; OnPropertyChanged(); _modelCurrentPage = 1; LoadModelsAsync(); } }
        
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
                        _modelCurrentPage = 1;
                        LoadModelsAsync(); 
                    }
                }
            } 
        }

        // Device Filters
        private string _deviceNameFilter = "";
        public string DeviceNameFilter { get => _deviceNameFilter; set { _deviceNameFilter = value; OnPropertyChanged(); _currentPage = 1; LoadDevicesAsync(); } }
        private string _deviceImeiFilter = "";
        public string DeviceImeiFilter { get => _deviceImeiFilter; set { _deviceImeiFilter = value; OnPropertyChanged(); _currentPage = 1; LoadDevicesAsync(); } }
        private string _deviceSnFilter = "";
        public string DeviceSnFilter { get => _deviceSnFilter; set { _deviceSnFilter = value; OnPropertyChanged(); _currentPage = 1; LoadDevicesAsync(); } }

        // Model Pagination
        private int _modelPageSize = 10;
        public int ModelPageSize { get => _modelPageSize; set { _modelPageSize = value; OnPropertyChanged(); LoadModelsAsync(); } }
        private int _modelTotalCount;
        public int ModelTotalCount { get => _modelTotalCount; set { _modelTotalCount = value; OnPropertyChanged(); OnPropertyChanged(nameof(ModelTotalPages)); OnPropertyChanged(nameof(CanGoModelPrev)); OnPropertyChanged(nameof(CanGoModelNext)); } }
        public int ModelTotalPages => (int)Math.Ceiling((double)ModelTotalCount / Math.Max(1, ModelPageSize));
        public bool CanGoModelPrev => ModelCurrentPage > 1;
        public bool CanGoModelNext => ModelCurrentPage < ModelTotalPages;
        private int _modelCurrentPage = 1;
        public int ModelCurrentPage { get => _modelCurrentPage; set { _modelCurrentPage = value; OnPropertyChanged(); OnPropertyChanged(nameof(CanGoModelPrev)); OnPropertyChanged(nameof(CanGoModelNext)); LoadModelsAsync(); } }

        // Device Pagination
        private int _pageSize = 10;
        public int PageSize { get => _pageSize; set { _pageSize = value; OnPropertyChanged(); LoadDevicesAsync(); } }
        private int _totalCount;
        public int TotalCount { get => _totalCount; set { _totalCount = value; OnPropertyChanged(); OnPropertyChanged(nameof(TotalPages)); OnPropertyChanged(nameof(CanGoPrev)); OnPropertyChanged(nameof(CanGoNext)); } }
        public int TotalPages => (int)Math.Ceiling((double)TotalCount / Math.Max(1, PageSize));
        public bool CanGoPrev => CurrentPage > 1;
        public bool CanGoNext => CurrentPage < TotalPages;
        private int _currentPage = 1;
        public int CurrentPage { get => _currentPage; set { _currentPage = value; OnPropertyChanged(); OnPropertyChanged(nameof(CanGoPrev)); OnPropertyChanged(nameof(CanGoNext)); LoadDevicesAsync(); } }

        public MainViewModel()
        {
            InitializeAsync();
            _syncTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(30) }; // Reduced sync frequency for stability
            _syncTimer.Tick += (s, e) => { RefreshAllAsync(); };
            _syncTimer.Start();
        }

        private async void InitializeAsync()
        {
            try
            {
                ErrorMessage = "";
                await LoadCategoriesAsync();
                await LoadModelsAsync();
                await LoadDevicesAsync();
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Critical error during initialization: {ex.Message}";
            }
        }

        public async Task LoadCategoriesAsync()
        {
            try
            {
                var categories = await _deviceService.GetDistinctCategoriesAsync();
                var categoryList = categories.ToList();
                categoryList.Insert(0, "All");

                _isUpdatingCategories = true;
                ModelCategories.Clear();
                foreach (var cat in categoryList) ModelCategories.Add(cat);
                
                if (string.IsNullOrEmpty(ModelCategoryFilter) || !ModelCategories.Contains(ModelCategoryFilter))
                    ModelCategoryFilter = "All";
                
                _isUpdatingCategories = false;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading categories: {ex.Message}");
            }
        }

        public async Task LoadModelsAsync()
        {
            try
            {
                var (models, total) = await _deviceService.GetModelsPagedAsync(
                    ModelCurrentPage, 
                    ModelPageSize, 
                    ModelOrderBy, 
                    ModelIsDescending, 
                    ModelNameFilter, 
                    ModelManufacturerFilter, 
                    ModelCategoryFilter);
                
                Models.Clear();
                foreach (var m in models) Models.Add(m);
                ModelTotalCount = total;
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
            // Only refresh current pages
            await LoadModelsAsync();
            await LoadDevicesAsync();
            // Occasionally refresh categories too?
            _ = LoadCategoriesAsync();
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

        public async void ToggleDeviceBorrowStatus(Device device)
        {
            try
            {
                // Local Update for immediate feedback (Optimistic Update)
                var oldStatus = device.IsBorrowed;
                var modelId = device.ModelId;

                var success = await _deviceService.ToggleDeviceBorrowStatusAsync(device.Id);
                if (success)
                {
                    // Update the device status locally
                    device.IsBorrowed = !oldStatus;
                    
                    // Update corresponding model's available count if it exists in the current view
                    var model = Models.FirstOrDefault(m => m.Id == modelId);
                    if (model != null)
                    {
                        model.Available += (device.IsBorrowed ? -1 : 1);
                    }
                    
                    ErrorMessage = "";
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Error toggling device status: {ex.Message}";
                // Fallback: Reload all if out of sync
                RefreshAllAsync();
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? name = null) => 
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
