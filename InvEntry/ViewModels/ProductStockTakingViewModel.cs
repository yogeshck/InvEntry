using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DevExpress.Xpf.Editors;
using InvEntry.Models;
using InvEntry.Services;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows.Input;

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

        private ScanItem Items;
        private int Status;

        [ObservableProperty]
        private StockVerifyScan _stockVerifiedItem;

        [ObservableProperty]
        private int totalScanned;

        [ObservableProperty]
        private int _missingCount;

        public ObservableCollection<ScanItem> _recentScanned;

        public ProductStockTakingViewModel(IProductStockService productStockService,
            IStockVerifyScanService stockVerifyScanService)
        {
            _productStockService = productStockService;
            _stockVerifyScanService= stockVerifyScanService;

            ProductStockGridList = new ObservableCollection<ProductStock>();
            UnavailableItems = new ObservableCollection<ScanItem>();

            Items = new ScanItem();
            StockVerifiedItem = new StockVerifyScan();

            MissingCount = 0;
        }


        [RelayCommand(CanExecute = nameof(CanScan))]
        private async Task ProcessScanAsync(EditValueChangedEventArgs args)
        {

            Status = 0;

            var barcode = args.NewValue as string;
            if (string.IsNullOrEmpty(barcode))
                return;

           //if (_scannedSet.Contains(barcode))
           // {
                // duplicate logic
           // }
           // else
            {
             //   _scannedSet.Add(barcode);
                TotalScanned++;
                await FetchProductAsync(barcode);

            }

            StockVerifiedItem = new();

            StockVerifiedItem.Barcode = barcode;
            StockVerifiedItem.SessionId = Status;

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
            ProductSkuStock = await _productStockService.GetProductStock(barcode);
            if (ProductSkuStock is not null)
            {
                // IsBarCodeEnabled = true;
                // ProductIdUI = ProductSkuStock.Category;

                ProductStockGridList.Add(ProductSkuStock);

                Status = 1;   //1  verified and available in table

            } else
            {
                Items.Barcode = barcode;

                UnavailableItems.Add(Items);
                MissingCount++;
            };

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