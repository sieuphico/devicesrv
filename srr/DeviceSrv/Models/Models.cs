using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace DeviceSrv.Models
{
    public class Model : INotifyPropertyChanged
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Manufacturer { get; set; }
        public string Category { get; set; }
        public string Subcategory { get; set; }
        
        private int _available;
        public int Available
        {
            get => _available;
            set { _available = value; OnPropertyChanged(); }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? name = null) => 
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }

    public class Device : INotifyPropertyChanged
    {
        public int Id { get; set; }
        public int ModelId { get; set; }
        public string Name { get; set; }
        public string Imei { get; set; }
        public string SerialLab { get; set; }
        public string SerialNumber { get; set; }
        public string Cicuiseri { get; set; }
        public string Hwversion { get; set; }
        
        private bool _isBorrowed;
        public bool IsBorrowed
        {
            get => _isBorrowed;
            set { _isBorrowed = value; OnPropertyChanged(); }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? name = null) => 
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
