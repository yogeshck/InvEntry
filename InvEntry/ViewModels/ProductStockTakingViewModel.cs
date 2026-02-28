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
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows.Input;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;

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
        private StockVerifyScan _stockVerifiedItem;

        [ObservableProperty]
        private int totalScanned;

        [ObservableProperty]
        private int _missingCount;

        private ScanItem Items;
        private string Status;
        private long SessionId;

        public ObservableCollection<ScanItem> _recentScanned;

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
            SessionId = long.Parse(DateTime.Now.ToString("MMddsssss"));

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
                _messageBoxService.ShowMessage("Invalid Tag", "Invalid Tag", MessageButton.OK);
                return;
            }
            
            if (_scannedSet.Contains(barcode))
            {
                _messageBoxService.ShowMessage("Tag already scanned", "Duplicate Tag", MessageButton.OK);
                //return;
                //there are some duplicate tags printed - till we make unique, return should be blocked here
                Status = "Dup Scan";
            }
             else
            {
                _scannedSet.Add(barcode);
                TotalScanned++;
                await FetchProductAsync(barcode);

            }

            StockVerifiedItem = new();

            StockVerifiedItem.Barcode = barcode;
            StockVerifiedItem.SessionId = SessionId;
            StockVerifiedItem.Status = Status;

            await AddStockVerifyScan(StockVerifiedItem);

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

                Status = ProductSkuStock.IsProductSold == false ? "In-Stock" : "Sold"; //1  In-Stock  2. Sold   3.Missing

            }
            else
            {
                Items.Barcode = barcode;
                Status = "No Tag";
                UnavailableItems.Add(Items);
                MissingCount++;
            }
            ;

        }

        private async Task AddStockVerifyScan(StockVerifyScan verifyScan)
        {
            await _stockVerifyScanService.CreateVerifiedStock(verifyScan);
        }

        public class ScanItem
        {
            public string Barcode { get; set; }
            public string Status { get; set; }
            public string ScanTime { get; set; }
        }

    }
}