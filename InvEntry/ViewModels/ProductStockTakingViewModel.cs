using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DevExpress.Mvvm;
using DevExpress.Xpf.Editors;
using DevExpress.Xpo;
using DevExpress.Xpo.Helpers;
using InvEntry.Models;
using InvEntry.Services;
using InvEntry.Utils;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Media;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows.Input;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;
using System.Linq;

namespace InvEntry.ViewModels
{
    public partial class ProductStockTakingViewModel : ObservableObject
    {
        [ObservableProperty]
        private string _barcodeInput;

        [ObservableProperty]
        private ObservableCollection<ProductStock> _productStockGridList;

        private ProductStock ProductSkuStock;
        private readonly IProductStockService _productStockService;
        private readonly IStockVerifyScanService _stockVerifyScanService;

        private readonly HashSet<string> _scannedSet = new();

        [ObservableProperty]
        private ObservableCollection<ScanItem> _unavailableItems;

        [ObservableProperty]
        private ObservableCollection<CategorySummary> _categorySummaries = new();

        [ObservableProperty]
        private StockVerifyScan _stockVerifiedItem;

        [ObservableProperty]
        private string _statusMessage;

        [ObservableProperty]
        private int totalScanned;

        [ObservableProperty]
        private Brush _statusBrush = Brushes.Wheat; // default

        [ObservableProperty]
        private int _missingCount;

        private ScanItem Items;
        private string Status;
        private long SessionId;

        public ObservableCollection<ScanItem> _recentScanned;

        private readonly List<StockVerifyScan> _scanBufferList = new();

        private const int BulkSaveThreshold = 4;

        private readonly IMessageBoxService _messageBoxService;

        public ProductStockTakingViewModel(IProductStockService productStockService,
            IStockVerifyScanService stockVerifyScanService,
            IMessageBoxService messageBoxService)
        {
            _productStockService = productStockService;
            _stockVerifyScanService = stockVerifyScanService;

            ProductStockGridList = new ObservableCollection<ProductStock>();
            UnavailableItems = new ObservableCollection<ScanItem>();

            Items = new ScanItem();
            StockVerifiedItem = new StockVerifyScan();
            SessionId = long.Parse(DateTime.Now.ToString("MMddHHmmssfff"));

            MissingCount = 0;
            _messageBoxService = messageBoxService;
        }


        [RelayCommand(CanExecute = nameof(CanScan))]
        private async Task ProcessScanAsync(EditValueChangedEventArgs args)
        {

            Status = null;

            var barcode = args.NewValue as string;
            if (string.IsNullOrEmpty(barcode))
                return;

            BarcodeValidate scanInput = new();
            barcode = scanInput.Validate(barcode);

            if (string.IsNullOrEmpty(barcode) || barcode.Length < 9 || barcode.Length > 15)
            {
                StatusMessage = $"❌ Invalid Tag: {barcode}";
                StatusBrush = Brushes.Red;
                // _messageBoxService.ShowMessage("Invalid Tag", "Invalid Tag", MessageButton.OK);
                return;
            }

            if (_scannedSet.Contains(barcode))
            {
                StatusMessage = $"Tag already scanned: {barcode}";
                StatusBrush = Brushes.Red;
                //_messageBoxService.ShowMessage("Tag already scanned", "Duplicate Tag", MessageButton.OK);
                //return;
                //there are some duplicate tags printed - till we make unique, return should be blocked here
                Status = "Dup Scan";
            }
            else
            {
                _scannedSet.Add(barcode);
                TotalScanned++;
                await FetchProductAsync(barcode);
                StatusMessage = $"✅ Scanned: {barcode}";
                StatusBrush = Brushes.Green;


            }

            StockVerifiedItem = new StockVerifyScan
            {
                Barcode = barcode,
                SessionId = SessionId,
                Status = Status,
            };

            _scanBufferList.Add(StockVerifiedItem);

            if (_scanBufferList.Count >= BulkSaveThreshold)
            {
                await BulkSaveAsync();
            }

            //Disabled individual save for now - we will do bulk save every 50 scans or on demand
            //await AddStockVerifyScan(StockVerifiedItem);

            BarcodeInput = string.Empty;
        }

        private bool CanScan(EditValueChangedEventArgs args)
        {

            return args.NewValue is not null && args.NewValue is string barcode &&
                !string.IsNullOrWhiteSpace(barcode);
        }


        private async Task FetchProductAsync(string barcode)
        {

            ProductSkuStock = new();
            ProductSkuStock = await _productStockService.GetProduct(barcode);
            if (ProductSkuStock is not null)
            {
                // IsBarCodeEnabled = true;
                // ProductIdUI = ProductSkuStock.Category;

                ProductStockGridList.Add(ProductSkuStock);
                UpdateCategorySummary(ProductSkuStock);

                Status = ProductSkuStock.IsProductSold == false ? "In-Stock" : "Sold"; //1  In-Stock  2. Sold   3.Missing

            }
            else
            {
                var missingItem = new ScanItem()
                {
                    Barcode = barcode,
                    Status = "No Tag",
                        ScanTime = DateTime.Now.ToString("HH:mm:ss")
                };
                Status = "No Tag";
                UnavailableItems.Add(missingItem);
                MissingCount++;
            }
            ;

        }

        private void UpdateCategorySummary(ProductStock product)
        {
            var existing = CategorySummaries.FirstOrDefault(c => c.Category == product.Category);
            if (existing != null)
            {
                existing.ItemCount++;
                existing.TotalWeight += product.NetWeight;
            }
            else
            {
                CategorySummaries.Add(new CategorySummary
                {
                    Category = product.Category,
                    ItemCount = 1,
                    TotalWeight = product.NetWeight,
                });
            }
        }


        private async Task AddStockVerifyScan(StockVerifyScan verifyScan)
        {
            await _stockVerifyScanService.CreateVerifiedStock(verifyScan);
        }




        private async Task BulkSaveAsync()
        {
            try
            {
                await _stockVerifyScanService.CreateVerifiedStockBulk(_scanBufferList);
                StatusMessage = $"💾 Saved {_scanBufferList.Count} scans to DB";
                _scanBufferList.Clear();
            }
            catch (Exception ex)
            {
                StatusMessage = $"❌ Save failed: {ex.Message}";
            }
        }


        public class ScanItem
        {
            public string Barcode { get; set; }
            public string Status { get; set; }
            public string ScanTime { get; set; }
        }

    }

    public partial class CategorySummary : ObservableObject
    {
        [ObservableProperty]
        public string _category;

        [ObservableProperty]
        public int _itemCount; 

        [ObservableProperty]
        public decimal? _totalWeight;     }

}