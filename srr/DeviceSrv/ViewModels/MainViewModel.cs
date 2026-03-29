using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Microsoft.UI.Xaml;

namespace DeviceSrv.ViewModels
{
    public class MainViewModel : INotifyPropertyChanged
    {
        private DispatcherTimer _syncTimer;

        public ModelListViewModel ModelVM { get; } = new();
        public DeviceListViewModel DeviceVM { get; } = new();

        public MainViewModel()
        {
            // Cross-communication
            ModelVM.DeviceBorrowed += () => DeviceVM.LoadDevicesAsync();
            DeviceVM.DeviceToggled += (modelId, isBorrowed) => ModelVM.ProcessDeviceToggle(modelId, isBorrowed);

            // Global Sync
            _syncTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(30) };
            _syncTimer.Tick += (s, e) => { RefreshAllAsync(); };
            _syncTimer.Start();
        }

        public async void RefreshAllAsync()
        {
            await ModelVM.LoadModelsAsync();
            await DeviceVM.LoadDevicesAsync();
            _ = ModelVM.LoadCategoriesAsync();
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? name = null) => 
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}

