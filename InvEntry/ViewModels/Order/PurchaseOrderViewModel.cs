using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows.Input;

namespace InvEntry.ViewModels.Order
{
    public partial class PurchaseOrderViewModel : ObservableObject
    {
        public PurchaseOrderViewModel()
        {
            PurchaseDate = DateTime.Now;
            OrderNo = GenerateOrderNo();
            PurchaseItems = new ObservableCollection<PurchaseItem>();
            MetalTypes = new ObservableCollection<string> { "Gold", "Silver", "Platinum" };
            IDProofTypes = new ObservableCollection<string> { "Aadhaar", "Passport", "Driving License" };

            AddItemCommand = new RelayCommand(AddItem);
            SaveCommand = new RelayCommand(Save, CanSave);
            ResetCommand = new RelayCommand(Reset);
            CloseCommand = new RelayCommand(Close);
            DeleteItemCommand = new RelayCommand(DeleteItem, CanDelete);
        }

        // Customer Info
        private string _customerName;
        public string CustomerName
        {
            get => _customerName;
            set { _customerName = value; OnPropertyChanged(); }
        }

        private string _contactNo;
        public string ContactNo
        {
            get => _contactNo;
            set { _contactNo = value; OnPropertyChanged(); }
        }

        private string _address;
        public string Address
        {
            get => _address;
            set { _address = value; OnPropertyChanged(); }
        }

        public ObservableCollection<string> IDProofTypes { get; }
        private string _idProofType;
        public string IDProofType
        {
            get => _idProofType;
            set { _idProofType = value; OnPropertyChanged(); }
        }

        private string _idProofNumber;
        public string IDProofNumber
        {
            get => _idProofNumber;
            set { _idProofNumber = value; OnPropertyChanged(); }
        }

        // Order Info
        public DateTime PurchaseDate { get; set; }
        public string OrderNo { get; set; }
        public string Remarks { get; set; }

        // Items
        public ObservableCollection<PurchaseItem> PurchaseItems { get; }
        public ObservableCollection<string> MetalTypes { get; }

        private PurchaseItem _selectedItem;
        public PurchaseItem SelectedItem
        {
            get => _selectedItem;
            set { _selectedItem = value; OnPropertyChanged(); }
        }

        public decimal TotalValue
        {
            get
            {
                decimal sum = 0;
                foreach (var item in PurchaseItems)
                    sum += item.TotalValue;
                return sum;
            }
        }

        // Commands
        public ICommand AddItemCommand { get; }
        public ICommand SaveCommand { get; }
        public ICommand ResetCommand { get; }
        public ICommand CloseCommand { get; }
        public ICommand DeleteItemCommand { get; }

        // Methods
        private string GenerateOrderNo()
        {
            return $"PO-{DateTime.Now:yyyyMMddHHmmss}";
        }

        private bool CanSave(object parameter)
        {
            return !string.IsNullOrWhiteSpace(CustomerName) && PurchaseItems.Count > 0;
        }

        private void Save(object parameter)
        {
            // TODO: Persist to DB or service
            System.Windows.MessageBox.Show($"Purchase Order {OrderNo} saved. Total Value: {TotalValue}");
        }

        private void Reset(object parameter)
        {
            CustomerName = string.Empty;
            ContactNo = string.Empty;
            Address = string.Empty;
            IDProofType = string.Empty;
            IDProofNumber = string.Empty;
            Remarks = string.Empty;
            PurchaseItems.Clear();
            OrderNo = GenerateOrderNo();
            OnPropertyChanged(nameof(TotalValue));
        }

        private void Close(object parameter)
        {
            // TODO: Implement window close logic via event/messaging
        }

        private void AddItem(object parameter)
        {
            var newItem = new PurchaseItem
            {
                MetalType = "Gold",
                Purity = 91.6m,
                Weight = 0,
                RatePerGram = 0
            };
            PurchaseItems.Add(newItem);
            SelectedItem = newItem;
            OnPropertyChanged(nameof(TotalValue));
        }

        private bool CanDelete(object parameter) => SelectedItem != null;
        private void DeleteItem(object parameter)
        {
            if (SelectedItem != null)
            {
                PurchaseItems.Remove(SelectedItem);
                OnPropertyChanged(nameof(TotalValue));
            }
        }

        // INotifyPropertyChanged
        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

            if (propertyName == nameof(PurchaseItems) || propertyName == nameof(SelectedItem))
                OnPropertyChanged(nameof(TotalValue));
        }
    }

    // Purchase Item Model
    public class PurchaseItem : INotifyPropertyChanged
    {
        private string _metalType;
        public string MetalType
        {
            get => _metalType;
            set { _metalType = value; OnPropertyChanged(); }
        }

        private decimal _purity;
        public decimal Purity
        {
            get => _purity;
            set { _purity = value; OnPropertyChanged(); }
        }

        private decimal _weight;
        public decimal Weight
        {
            get => _weight;
            set { _weight = value; OnPropertyChanged(); OnPropertyChanged(nameof(TotalValue)); }
        }

        private decimal _ratePerGram;
        public decimal RatePerGram
        {
            get => _ratePerGram;
            set { _ratePerGram = value; OnPropertyChanged(); OnPropertyChanged(nameof(TotalValue)); }
        }

        public decimal TotalValue => Weight * RatePerGram;

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    // Simple RelayCommand Implementation
    public class RelayCommand : ICommand
    {
        private readonly Action<object> _execute;
        private readonly Func<object, bool> _canExecute;

        public RelayCommand(Action<object> execute, Func<object, bool> canExecute = null)
        {
            _execute = execute;
            _canExecute = canExecute;
        }

        public bool CanExecute(object parameter) => _canExecute == null || _canExecute(parameter);
        public void Execute(object parameter) => _execute(parameter);
        public event EventHandler CanExecuteChanged
        {
            add => CommandManager.RequerySuggested += value;
            remove => CommandManager.RequerySuggested -= value;
        }
    }
}