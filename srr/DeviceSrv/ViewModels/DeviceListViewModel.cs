using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using DeviceSrv.Models;
using DeviceSrv.Services;

namespace DeviceSrv.ViewModels
{
    public class DeviceListViewModel : FilterableViewModel
    {
        private readonly DeviceService _deviceService = new();

        public ObservableCollection<Device> Devices { get; } = new();

        // Sorting
        private string _deviceOrderBy = "Id";
        public string DeviceOrderBy { get => _deviceOrderBy; set => SetProperty(ref _deviceOrderBy, value); }
        
        private bool _deviceIsDescending = false;
        public bool DeviceIsDescending { get => _deviceIsDescending; set => SetProperty(ref _deviceIsDescending, value); }

        // Pagination
        private int _pageSize = 10;
        public int PageSize 
        { 
            get => _pageSize; 
            set { if (SetProperty(ref _pageSize, value)) _ = LoadDevicesAsync(); } 
        }
        
        private int _totalCount;
        public int TotalCount 
        { 
            get => _totalCount; 
            set 
            { 
                if (SetProperty(ref _totalCount, value))
                {
                    OnPropertyChanged(nameof(TotalPages)); 
                    OnPropertyChanged(nameof(CanGoPrev)); 
                    OnPropertyChanged(nameof(CanGoNext));
                }
            } 
        }
        
        public int TotalPages => (int)Math.Ceiling((double)TotalCount / Math.Max(1, PageSize));
        public bool CanGoPrev => CurrentPage > 1;
        public bool CanGoNext => CurrentPage < TotalPages;
        
        private int _currentPage = 1;
        public int CurrentPage 
        { 
            get => _currentPage; 
            set 
            { 
                if (SetProperty(ref _currentPage, value))
                {
                    OnPropertyChanged(nameof(CanGoPrev)); 
                    OnPropertyChanged(nameof(CanGoNext)); 
                    _ = LoadDevicesAsync();
                }
            } 
        }

        public event Action<int, bool>? DeviceToggled;

        public DeviceListViewModel()
        {
            _ = InitializeAsync();
        }

        public async Task InitializeAsync()
        {
            try
            {
                ErrorMessage = "";
                await LoadDevicesAsync();
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Critical error during initialization: {ex.Message}";
            }
        }

        public void UpdateDeviceSort(string orderBy, bool isDescending)
        {
            DeviceOrderBy = orderBy;
            DeviceIsDescending = isDescending;
            CurrentPage = 1;
            _ = LoadDevicesAsync();
        }

        public override void UpdateFilter(string filterKey, string text)
        {
            FilterHeaders[filterKey] = text;
            if (filterKey.StartsWith("DeviceGrid"))
            {
                CurrentPage = 1;
                _ = LoadDevicesAsync();
            }
        }

        public override void ClearFilters()
        {
            FilterHeaders.Clear();
            CurrentPage = 1;
            _ = LoadDevicesAsync();
        }

        public async Task LoadDevicesAsync()
        {
            try
            {
                string dName = FilterHeaders.TryGetValue("DeviceGrid_Name", out var dn) ? dn : "";
                string dModel = FilterHeaders.TryGetValue("DeviceGrid_ModelName", out var dm) ? dm : "";
                string dImei = FilterHeaders.TryGetValue("DeviceGrid_Imei", out var di) ? di : "";
                string dSn = FilterHeaders.TryGetValue("DeviceGrid_SerialNumber", out var ds) ? ds : "";

                var combinedFilter = $"{dName} {dModel} {dImei} {dSn}".Trim();
                var (devices, total) = await _deviceService.GetDevicesPagedAsync(CurrentPage, PageSize, DeviceOrderBy, DeviceIsDescending, combinedFilter, onlyBorrowed: true);
                
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

        public async void ToggleDeviceBorrowStatus(Device device)
        {
            try
            {
                var oldStatus = device.IsBorrowed;
                var modelId = device.ModelId;

                var success = await _deviceService.ToggleDeviceBorrowStatusAsync(device.Id);
                if (success)
                {
                    device.IsBorrowed = !oldStatus;
                    DeviceToggled?.Invoke(modelId, device.IsBorrowed);
                    ErrorMessage = "";

                    // If device was returned (now available), refresh list so it disappears
                    if (!device.IsBorrowed)
                    {
                        _ = LoadDevicesAsync();
                    }
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Error toggling device status: {ex.Message}";
                _ = LoadDevicesAsync();
            }
        }
    }
}
