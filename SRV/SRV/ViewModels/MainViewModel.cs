using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Npgsql;
using SRV.Data;
using SRV.Services;
using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;

namespace SRV.ViewModels
{
    public partial class ModelDto : ObservableObject
    {
        public Model Data { get; }
        public ModelDto(Model data) { Data = data; }

        [ObservableProperty]
        private int _quantityToBorrow = 1;
    }

    public partial class MainViewModel : ObservableObject
    {
        private readonly IDeviceService _deviceService;
        private readonly Microsoft.UI.Dispatching.DispatcherQueue _dispatcherQueue;

        public MainViewModel(IDeviceService deviceService)
        {
            _deviceService = deviceService;
            _dispatcherQueue = Microsoft.UI.Dispatching.DispatcherQueue.GetForCurrentThread();

            Task.Run(InitializeAndListenAsync);
        }

        private async Task InitializeAndListenAsync()
        {
            await _deviceService.EnsureDatabaseSetupAsync();
            
            _dispatcherQueue.TryEnqueue(() =>
            {
                LoadModelsCommand.Execute(null);
                LoadDevicesCommand.Execute(null);
            });

            // Lắng nghe sự kiện đồng bộ đa instance từ PostgreSQL
            var connStr = "Host=localhost;Port=5432;Database=DeviceSrvDb;Username=postgres;Password=root";
            var conn = new NpgsqlConnection(connStr);
            await conn.OpenAsync();
            
            conn.Notification += (o, e) =>
            {
                _dispatcherQueue.TryEnqueue(() =>
                {
                    LoadModelsCommand.Execute(null);
                    LoadDevicesCommand.Execute(null);
                });
            };
            
            using (var cmd = new NpgsqlCommand("LISTEN db_change;", conn))
            {
                await cmd.ExecuteNonQueryAsync();
            }
            
            while (true)
            {
                try { await conn.WaitAsync(); }
                catch { break; }
            }
        }

        // --- MODEL TAB ---
        public ObservableCollection<ModelDto> Models { get; } = new ObservableCollection<ModelDto>();

        [ObservableProperty] private string _modelCategoryFilter = "";
        [ObservableProperty] private string _modelNameFilter = "";
        [ObservableProperty] private string _modelManufacturerFilter = "";

        [ObservableProperty] private string _modelSortColumn = "";
        [ObservableProperty] private bool _modelSortAscending = true;

        [ObservableProperty] private int _modelPage = 1;
        [ObservableProperty] private int _modelTotalPages = 1;
        public int ModelPageSize { get; private set; } = 20;

        [ObservableProperty] private string _modelSelectedPageSize = "20";
        partial void OnModelSelectedPageSizeChanged(string value)
        {
            if (int.TryParse(value, out int size)) { ModelPageSize = size; ModelPage = 1; LoadModelsCommand.Execute(null); }
        }

        [ObservableProperty] private string _modelPageInfoText = "Trang 1 / 1";
        [ObservableProperty] private bool _canGoModelPrev;
        [ObservableProperty] private bool _canGoModelNext;

        [RelayCommand]
        public async Task LoadModelsAsync()
        {
            var (models, total) = await _deviceService.GetWarehouseModelsAsync(
                ModelCategoryFilter, ModelNameFilter, ModelManufacturerFilter, 
                ModelSortColumn, ModelSortAscending, ModelPage, ModelPageSize);
            
            ModelTotalPages = (int)Math.Ceiling((double)total / ModelPageSize);
            if (ModelTotalPages < 1) ModelTotalPages = 1;
            if (ModelPage > ModelTotalPages) ModelPage = ModelTotalPages;

            Models.Clear();
            foreach (var m in models) Models.Add(new ModelDto(m));

            ModelPageInfoText = $"Trang {ModelPage} / {ModelTotalPages} (Tổng: {total:N0})";
            CanGoModelPrev = ModelPage > 1;
            CanGoModelNext = ModelPage < ModelTotalPages;
        }

        [RelayCommand]
        private void SortModels(string column)
        {
            if (ModelSortColumn == column) ModelSortAscending = !ModelSortAscending;
            else { ModelSortColumn = column; ModelSortAscending = true; }
            ModelPage = 1;
            LoadModelsCommand.Execute(null);
        }

        [RelayCommand]
        private async Task BorrowAsync(ModelDto dto)
        {
            if (dto == null || dto.QuantityToBorrow <= 0 || dto.QuantityToBorrow > dto.Data.Available) return;
            await _deviceService.BorrowDevicesAsync(dto.Data.Id, dto.QuantityToBorrow);
            // Sẽ tự động trigger Load qua LISTEN pattern.
        }

        [RelayCommand] private void SearchModels() { ModelPage = 1; LoadModelsCommand.Execute(null); }
        [RelayCommand] private void ModelFirstPage() { ModelPage = 1; LoadModelsCommand.Execute(null); }
        [RelayCommand] private void ModelPrevPage() { if (ModelPage > 1) ModelPage--; LoadModelsCommand.Execute(null); }
        [RelayCommand] private void ModelNextPage() { if (ModelPage < ModelTotalPages) ModelPage++; LoadModelsCommand.Execute(null); }

        // --- DEVICE TAB ---
        public ObservableCollection<Device> Devices { get; } = new ObservableCollection<Device>();

        [ObservableProperty] private string _deviceNameFilter = "";
        [ObservableProperty] private string _deviceImeiFilter = "";
        [ObservableProperty] private string _deviceModelNameFilter = "";

        [ObservableProperty] private string _deviceSortColumn = "";
        [ObservableProperty] private bool _deviceSortAscending = true;

        [ObservableProperty] private int _devicePage = 1;
        [ObservableProperty] private int _deviceTotalPages = 1;
        public int DevicePageSize { get; private set; } = 20;

        [ObservableProperty] private string _deviceSelectedPageSize = "20";
        partial void OnDeviceSelectedPageSizeChanged(string value)
        {
            if (int.TryParse(value, out int size)) { DevicePageSize = size; DevicePage = 1; LoadDevicesCommand.Execute(null); }
        }

        [ObservableProperty] private string _devicePageInfoText = "Trang 1 / 1";
        [ObservableProperty] private bool _canGoDevicePrev;
        [ObservableProperty] private bool _canGoDeviceNext;

        [RelayCommand]
        public async Task LoadDevicesAsync()
        {
            var (devices, total) = await _deviceService.GetAllDevicesAsync(
                DeviceNameFilter, DeviceImeiFilter, DeviceModelNameFilter, 
                DeviceSortColumn, DeviceSortAscending, DevicePage, DevicePageSize);
            
            DeviceTotalPages = (int)Math.Ceiling((double)total / DevicePageSize);
            if (DeviceTotalPages < 1) DeviceTotalPages = 1;
            if (DevicePage > DeviceTotalPages) DevicePage = DeviceTotalPages;

            Devices.Clear();
            foreach (var d in devices) Devices.Add(d);

            DevicePageInfoText = $"Trang {DevicePage} / {DeviceTotalPages} (Tổng: {total:N0})";
            CanGoDevicePrev = DevicePage > 1;
            CanGoDeviceNext = DevicePage < DeviceTotalPages;
        }

        [RelayCommand]
        private void SortDevices(string column)
        {
            if (DeviceSortColumn == column) DeviceSortAscending = !DeviceSortAscending;
            else { DeviceSortColumn = column; DeviceSortAscending = true; }
            DevicePage = 1;
            LoadDevicesCommand.Execute(null);
        }

        [RelayCommand] private void SearchDevices() { DevicePage = 1; LoadDevicesCommand.Execute(null); }
        [RelayCommand] private void DeviceFirstPage() { DevicePage = 1; LoadDevicesCommand.Execute(null); }
        [RelayCommand] private void DevicePrevPage() { if (DevicePage > 1) DevicePage--; LoadDevicesCommand.Execute(null); }
        [RelayCommand] private void DeviceNextPage() { if (DevicePage < DeviceTotalPages) DevicePage++; LoadDevicesCommand.Execute(null); }
    }
}
