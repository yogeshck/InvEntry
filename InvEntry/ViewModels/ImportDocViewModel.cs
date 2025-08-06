using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DevExpress.Mvvm;
using InvEntry.Extension;
using InvEntry.Managers;
using InvEntry.Mappers;
using InvEntry.Models;
using InvEntry.Models.Extensions;
using InvEntry.Models.Parsed;
using InvEntry.Services;
using InvEntry.Utils;
using Microsoft.Win32;
using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace InvEntry.ViewModels
{
    public partial class ImportDocViewModel : ObservableObject
    {
        [ObservableProperty]
        private EstimateHeader _estimate;

        [ObservableProperty]
        private ObservableCollection<ProductView> _productViewList;

        private readonly IStockManager _stockManager;
        private readonly IProductViewService _productViewService;
        private readonly IProductStockSummaryService _stockSumryService;
        private readonly IProductService _productService;
        private readonly IDialogService _dialogService;

        //     private readonly IStockManager _stockManager;

        [ObservableProperty]
        private string? selectedFilePath;

      //  public ObservableCollection<ProductView> ProductViewList { get; set; } = new();

        public ImportDocViewModel(IProductService productService, 
                                  IProductStockSummaryService stockSumryService,
                                  IProductViewService productViewService,
                                  IStockManager stockManager,
                                  IDialogService dialogService ) //, IStockManager stockManager)
        {
            _productService = productService;
            _stockSumryService = stockSumryService;
            _productViewService = productViewService;
            _stockManager = stockManager;
            _dialogService = dialogService;

            PopulateProductViewList();

            Estimate = new();
        }

        private async void PopulateProductViewList()
        {

            //ProductViewList.Clear();

            var list = await _productViewService.GetAll();

            if (list is not null)
            {
                ProductViewList = new(list);
            }
        }

        [RelayCommand]
        public void BrowseFile()
        {
            var dialog = new OpenFileDialog
            {
                Filter = "PDF files (*.pdf)|*.pdf",
                Multiselect = false
            };
            if (dialog.ShowDialog() == true)
            {
                SelectedFilePath = dialog.FileName;
            }
        }

        [RelayCommand]
        private void PdfDrop(DragEventArgs args)
        {
            if (!args.Data.GetDataPresent(DataFormats.FileDrop)) return;

            string[] files = (string[])args.Data.GetData(DataFormats.FileDrop);
            string pdfPath = files.FirstOrDefault(x => x.EndsWith(".pdf"));
            if (!string.IsNullOrEmpty(pdfPath))
                ExtractInvoiceWithOcr(pdfPath);

            UpdateStock();
        }

        [RelayCommand]
        private void FileImportButton()
        {

            OpenFileDialog openFileDialog = new OpenFileDialog
            {
                Filter = "Pdf files (*.pdf)|*.pdf|All files (*.*)|*.*",
                InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                Title = "Select a File to Import",
            };

            if (openFileDialog.ShowDialog() == true)
            {
                string filePath = openFileDialog.FileName;
                MessageBox.Show($"Selected file: {filePath}");
                ExtractInvoiceWithOcr(filePath);

                UpdateStock();
            }

        }

        private async void UpdateStock()
        {
            if (Estimate == null || Estimate.Lines == null) return;

            foreach (var line in Estimate.Lines)
            {
                var pview = ProductViewList.FirstOrDefault(x => x.Name.Equals(line.ProductName));
                if (pview != null)
                {
                    var pstk = await _stockSumryService.GetByProductGkey(pview.GKey);
                    pstk.EnrichStockFromEstimate(line);
                    await _stockManager.ApplyMaterialReceipt(pstk);
                }
            }
        }

        private async void ExtractInvoiceWithOcr(string SelectedFilePath)
        {
            if (string.IsNullOrEmpty(SelectedFilePath))
                return;

            var ocrReader = new PdfOcrReader();
            var rawText = await ocrReader.ExtractTextAsync(SelectedFilePath);

            var (parsedHeader, parsedLines) = DocumentParser.ParseDocument<ParsedEstimateHeader, ParsedEstimateLine>(rawText, "estimate");

            Estimate = EstimateMapper.ToEstimateHeader(parsedHeader);
            Estimate.Lines = new ObservableCollection<EstimateLine>(EstimateMapper.ToEstimateLines(parsedLines));
        }


        [RelayCommand]
        private void Review()
        {

            UpdateStock();

            // Show Review Popup and Confirm Save
            var vm = new ReviewPopupViewModel(Estimate);
            var view = new Views.ReviewPopupView { DataContext = vm };

            var result = _dialogService.ShowDialog(MessageButton.OKCancel, "Product",
                                                "ReviewPopupView", vm);

            if (vm.Confirmed)
            {
            // API call placeholder -- need to implement actual API call logic
                    MessageBox.Show("Estimate confirmed. Send to API here.");

            //        UpdateStock();
            }


            /*    private ProductView? ProductStockSelection()
        {

            var vm = DISource.Resolve<InvoiceProductSelectionViewModel>();
            vm.Category = ProductIdUI;

            var result = _dialogService.ShowDialog(MessageButton.OKCancel, "Product",
                                                            "InvoiceProductSelectionView", vm);

            if (result == MessageResult.OK)
            {
                return vm.SelectedProduct;
            }
            return null;
        }*/
        }

    }

}

