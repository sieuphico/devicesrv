using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace DeviceSrv.ViewModels
{
    public abstract class FilterableViewModel : INotifyPropertyChanged
    {
        public Dictionary<string, ObservableCollection<string>> FilterOptions { get; } = new();
        public Dictionary<string, string> FilterHeaders { get; } = new();

        private string _errorMessage = "";
        public string ErrorMessage 
        { 
            get => _errorMessage; 
            set 
            { 
                _errorMessage = value; 
                OnPropertyChanged(); 
                OnPropertyChanged(nameof(HasError)); 
            } 
        }
        public bool HasError => !string.IsNullOrEmpty(ErrorMessage);

        public abstract void UpdateFilter(string filterKey, string text);
        public abstract void ClearFilters();

        public event PropertyChangedEventHandler? PropertyChanged;
        
        protected bool SetProperty<T>(ref T backingStore, T value, [CallerMemberName] string propertyName = "", System.Action? onChanged = null)
        {
            if (EqualityComparer<T>.Default.Equals(backingStore, value)) return false;
            backingStore = value;
            onChanged?.Invoke();
            OnPropertyChanged(propertyName);
            return true;
        }

        protected void OnPropertyChanged([CallerMemberName] string propertyName = "") => 
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
