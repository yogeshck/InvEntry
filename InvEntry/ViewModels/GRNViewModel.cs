using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DevExpress.Mvvm;
using DevExpress.Mvvm.Native;
using DevExpress.Xpf.Core;
using DevExpress.Xpf.Editors;
using DevExpress.Xpf.Grid;
using DevExpress.Xpf.Printing;
using InvEntry.Extension;
using InvEntry.Models;
using InvEntry.Models.Extensions;
using InvEntry.Reports;
using InvEntry.Services;
using InvEntry.Store;
using InvEntry.Utils;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using IDialogService = DevExpress.Mvvm.IDialogService;

namespace InvEntry.ViewModels
{
    public partial class GRNViewModel : ObservableObject
    {
        [ObservableProperty]
        private GrnHeader _header;

        [ObservableProperty]
        private string _categoryUI;

        [ObservableProperty]
        private ObservableCollection<string> productCategoryList;

        [ObservableProperty]
        private ObservableCollection<GrnLine> selectedRows;

        private readonly IGrnService _grnService;
        private readonly IProductCategoryService _productCategoryService;
        //private readonly ICustomerService _customerService;
        private readonly IProductService _productService;
        //private readonly IProductStockService _productStockService;
        private readonly IDialogService _dialogService;
        //private readonly IDialogService _reportDialogService;
        private readonly IMessageBoxService _messageBoxService;

        public GRNViewModel(IGrnService             grnService,
                            IProductService         productService ,
                            IDialogService          dialogService,
                            IProductCategoryService productCategoryService,
                            IMessageBoxService      messageBoxService   )
        {
            _grnService = grnService;
            _productService = productService;
            _dialogService = dialogService;
            _productCategoryService = productCategoryService;
            _messageBoxService = messageBoxService;

            PopulateProductCategoryList();

            SetHeader();

        }

        private void SetHeader()
        {
            Header = new()
            {
                GrnDate = DateTime.Now
            };
        }

        private async void PopulateProductCategoryList()
        {
            var list = await _productCategoryService.GetProductCategoryList();
            ProductCategoryList = new(list.Select(x => x.Name));
        }

        [RelayCommand]
        private async Task FetchSupplier(EditValueChangedEventArgs args)
        {

         //   if (string.IsNullOrEmpty(productIdUI)) return;

            var product = await _productService.GetByCategory(CategoryUI);

            GrnLine grnLine = new GrnLine()
            {
                ProductGkey    = product.GKey,
                ProductDesc    = product.Description,
                ProductPurity  = product.Purity
            };

            //grnLine.SetProductDetails(productStk);

            Header.GrnLines.Add(grnLine);

        }

        [RelayCommand]
        private void CellUpdate(CellValueChangedEventArgs args)
        {
            if (args.Row is GrnLine line)
            {
               // EvaluateFormula(line);
            }
        }

        [RelayCommand]
        private async Task Submit()
        {
            var header = await _grnService.CreateHeader(Header);

            if (header is not null)
            {
                Header.GKey = header.GKey;
                Header.GrnNbr = header.GrnNbr;

                Header.GrnLines.ForEach(x =>
                {
                    x.GrnHdrGkey    = header.GKey;
                    x.LineNbr       = Header.GrnLines.IndexOf(x) + 1;
                 //   x.OrderedQty    = grnLine;
                });

                // loop for validation check for customer
                await _grnService.CreateGrnLine(Header.GrnLines);

                _messageBoxService.ShowMessage( "GRN " + Header.GrnNbr + " Created Successfully",
                                                "GRN Creation", 
                                                MessageButton.OK, 
                                                MessageIcon.Exclamation);

                ResetGRN();

            }
        }

        private void ResetGRN()
        {

            SetHeader();

        }

    }
}
