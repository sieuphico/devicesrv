using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using DeviceSrv.Models;
using DeviceSrv.Services;

namespace DeviceSrv.ViewModels
{
    public class ModelListViewModel : FilterableViewModel
    {
        private readonly DeviceService _deviceService = new();

        public ObservableCollection<Model> Models { get; } = new();

        // Sorting
        private string _modelOrderBy = "Id";
        public string ModelOrderBy { get => _modelOrderBy; set => SetProperty(ref _modelOrderBy, value); }
        
        private bool _modelIsDescending = false;
        public bool ModelIsDescending { get => _modelIsDescending; set => SetProperty(ref _modelIsDescending, value); }

        // Pagination
        private int _modelPageSize = 10;
        public int ModelPageSize 
        { 
            get => _modelPageSize; 
            set { if (SetProperty(ref _modelPageSize, value)) _ = LoadModelsAsync(); } 
        }
        
        private int _modelTotalCount;
        public int ModelTotalCount 
        { 
            get => _modelTotalCount; 
            set 
            { 
                if (SetProperty(ref _modelTotalCount, value))
                {
                    OnPropertyChanged(nameof(ModelTotalPages)); 
                    OnPropertyChanged(nameof(CanGoModelPrev)); 
                    OnPropertyChanged(nameof(CanGoModelNext));
                }
            } 
        }
        
        public int ModelTotalPages => (int)Math.Ceiling((double)ModelTotalCount / Math.Max(1, ModelPageSize));
        public bool CanGoModelPrev => ModelCurrentPage > 1;
        public bool CanGoModelNext => ModelCurrentPage < ModelTotalPages;
        
        private int _modelCurrentPage = 1;
        public int ModelCurrentPage 
        { 
            get => _modelCurrentPage; 
            set 
            { 
                if (SetProperty(ref _modelCurrentPage, value))
                {
                    OnPropertyChanged(nameof(CanGoModelPrev)); 
                    OnPropertyChanged(nameof(CanGoModelNext)); 
                    _ = LoadModelsAsync();
                }
            } 
        }

        public event Action? DeviceBorrowed;

        public ModelListViewModel()
        {
            _ = InitializeAsync();
        }

        public async Task InitializeAsync()
        {
            try
            {
                ErrorMessage = "";
                await LoadCategoriesAsync();
                await LoadManufacturersAsync();
                await LoadModelsAsync();
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Critical error during initialization: {ex.Message}";
            }
        }

        public void UpdateModelSort(string orderBy, bool isDescending)
        {
            ModelOrderBy = orderBy;
            ModelIsDescending = isDescending;
            ModelCurrentPage = 1;
            _ = LoadModelsAsync();
        }

        public override void UpdateFilter(string filterKey, string text)
        {
            FilterHeaders[filterKey] = text;
            if (filterKey.StartsWith("ModelGrid"))
            {
                ModelCurrentPage = 1;
                _ = LoadModelsAsync();
            }
        }

        public async Task LoadCategoriesAsync()
        {
            try
            {
                var categories = await _deviceService.GetDistinctCategoriesAsync();
                var categoryList = categories.ToList();
                categoryList.Insert(0, "All");

                if (!FilterOptions.ContainsKey("ModelGrid_Category")) FilterOptions["ModelGrid_Category"] = new ObservableCollection<string>();
                var options = FilterOptions["ModelGrid_Category"];

                options.Clear();
                foreach (var cat in categoryList) options.Add(cat);
                
                if (!FilterHeaders.TryGetValue("ModelGrid_Category", out var currentVal) || !options.Contains(currentVal))
                {
                    FilterHeaders["ModelGrid_Category"] = "All";
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading categories: {ex.Message}");
            }
        }

        public async Task LoadManufacturersAsync()
        {
            try
            {
                var manufacturers = await _deviceService.GetDistinctManufacturersAsync();
                var manList = manufacturers.ToList();
                manList.Insert(0, "All");

                if (!FilterOptions.ContainsKey("ModelGrid_Manufacturer")) FilterOptions["ModelGrid_Manufacturer"] = new ObservableCollection<string>();
                var options = FilterOptions["ModelGrid_Manufacturer"];

                options.Clear();
                foreach (var man in manList) options.Add(man);
                
                if (!FilterHeaders.TryGetValue("ModelGrid_Manufacturer", out var currentVal) || !options.Contains(currentVal))
                {
                    FilterHeaders["ModelGrid_Manufacturer"] = "All";
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading manufacturers: {ex.Message}");
            }
        }

        public async Task LoadModelsAsync()
        {
            try
            {
                string nameFilter = FilterHeaders.TryGetValue("ModelGrid_Name", out var n) ? n : "";
                
                string manufacturerFilterRaw = FilterHeaders.TryGetValue("ModelGrid_Manufacturer", out var manf) ? manf : "";
                string manufacturerFilter = manufacturerFilterRaw == "All" ? "" : manufacturerFilterRaw;
                
                string categoryFilterRaw = FilterHeaders.TryGetValue("ModelGrid_Category", out var cat) ? cat : "All";
                string categoryFilter = string.IsNullOrEmpty(categoryFilterRaw) ? "All" : categoryFilterRaw;

                var (models, total) = await _deviceService.GetModelsPagedAsync(
                    ModelCurrentPage, 
                    ModelPageSize, 
                    ModelOrderBy, 
                    ModelIsDescending, 
                    nameFilter, 
                    manufacturerFilter, 
                    categoryFilter);
                
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

        public async void BorrowDevice(Model model, int quantity)
        {
            try
            {
                if (model.Available >= quantity)
                {
                    var success = await _deviceService.BorrowDevicesAsync(model.Id, quantity);
                    if (success)
                    {
                        _ = LoadModelsAsync();
                        DeviceBorrowed?.Invoke();
                    }
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Error borrowing device: {ex.Message}";
            }
        }

        public void ProcessDeviceToggle(int modelId, bool isBorrowed)
        {
            var model = Models.FirstOrDefault(m => m.Id == modelId);
            if (model != null)
            {
                model.Available += (isBorrowed ? -1 : 1);
            }
        }
    }
}
