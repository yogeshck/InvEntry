using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DevExpress.Mvvm;
using DevExpress.Mvvm.Native;
using DevExpress.Xpf.Core;
using DevExpress.Xpf.Editors;
using DevExpress.Xpf.Grid;
using DevExpress.Xpf.Printing;
using DevExpress.XtraPivotGrid.Data;
using InvEntry.Extension;
using InvEntry.Helper;
using InvEntry.Helpers;
using InvEntry.Models;
using InvEntry.Models.Extensions;
using InvEntry.Reports;
using InvEntry.Services;
using InvEntry.Store;
using InvEntry.Tally;
using InvEntry.Utils;
using InvEntry.Utils.Options;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Drawing.Text;
using System.Linq;
using System.Threading.Tasks;
using IDialogService = DevExpress.Mvvm.IDialogService;

namespace InvEntry.ViewModels;


public partial class OldMetalPurchaseViewModel : ObservableObject
{
    [ObservableProperty]
    private string _customerPhoneNumber;

    private ProductStock ProductSkuStock;

    [ObservableProperty]
    private string _customerState;

    /*    [ObservableProperty]
        private LedgersHeader _ledgerHeader;*/

    [ObservableProperty]
    private string _productIdUI;

    [ObservableProperty]
    private OrgThisCompanyView _company;

    [ObservableProperty]
    private Customer _buyer;

    [ObservableProperty]
    private ObservableCollection<string> productCategoryList;

    [ObservableProperty]
    private ObservableCollection<OldMetalTransaction> selectedRows;

    [ObservableProperty]
    private ObservableCollection<string> _metalList;

    [ObservableProperty]
    private ObservableCollection<MtblReference> _mtblReferencesList;

    [ObservableProperty]
    private OldMetalTransaction omHeader;

    [ObservableProperty]
    private ProductView _oldMetalProduct;

    private readonly ReferenceLoader _referenceLoader;

    private readonly ILedgerService _ledgerService;
    private readonly IDialogService _dialogService;
    private readonly IVoucherService _voucherService;
    private readonly ICustomerService _customerService;
    private readonly IDialogService _reportDialogService;
    private readonly IMessageBoxService _messageBoxService;
    private readonly IProductViewService _productViewService;
    private readonly IReportFactoryService _reportFactoryService;
    private readonly IMtblReferencesService _mtblReferencesService;
    private readonly IProductCategoryService _productCategoryService;
    private readonly IOrgThisCompanyViewService _orgThisCompanyViewService;
    private readonly IOldMetalTransactionService _oldMetalTransactionService;
    private readonly IProductStockSummaryService _productStockSummaryService;

    private List<MtblReference> _gstTaxRefList;

    private SettingsPageViewModel _settingsPageViewModel;
    private Dictionary<string, Action<InvoiceLine, decimal?>> copyInvoiceExpression;
    private Dictionary<string, Action<InvoiceHeader, decimal?>> copyHeaderExpression;

    private bool createCustomer = false;

    private decimal IGSTPercent = 0M;
    private decimal SCGSTPercent = 3M;
    private decimal todaysRate;

    public OldMetalPurchaseViewModel(
                IDialogService dialogService,
                ILedgerService ledgerService,
                ICustomerService customerService,
                IMessageBoxService messageBoxService,
                IProductViewService productViewService,
                IProductStockService productStockService,
                IMtblReferencesService mtblReferencesService,
                SettingsPageViewModel settingsPageViewModel,
                IReportFactoryService reportFactoryService,
                IProductCategoryService productCategoryService,
                IProductTransactionService productTransactionService,
                IOrgThisCompanyViewService orgThisCompanyViewService,
                IOldMetalTransactionService oldMetalTransactionService,
                IProductStockSummaryService productStockSummaryService,
                ReferenceLoader referenceLoader,
    [FromKeyedServices("ReportDialogService")] IDialogService reportDialogService)
    {

        _dialogService = dialogService;
        _ledgerService = ledgerService;
        _customerService = customerService;
        _messageBoxService = messageBoxService;
        _productViewService = productViewService;
        _reportDialogService = reportDialogService;
        _reportFactoryService = reportFactoryService;
        _productCategoryService = productCategoryService;
        _productStockSummaryService = productStockSummaryService;
        _orgThisCompanyViewService = orgThisCompanyViewService;
        _oldMetalTransactionService = oldMetalTransactionService;
        _mtblReferencesService = mtblReferencesService;

        _referenceLoader = referenceLoader;

        //_productTransactionSummaryService = productTransactionSummaryService;

        selectedRows = new();
        //productStockList = new();

       _settingsPageViewModel = settingsPageViewModel;

        SetMetalPrice();
        SetHeader();
        SetThisCompany();
        //SetMasterLedger();   TODO

        _ = LoadReferencesAsync();

        PopulateProductCategoryList();
        //PopulateStateList();
        //PopulateUnboundLineDataMap();
        PopulateMtblRefNameList();
        PopulateMetalList();
        PopulateTaxList();
        //PopulateSalesPersonList();

        //PopulateUnboundHeaderDataMap();
    }

    private async void SetThisCompany()
    {
        Company = new();
        Company = await _orgThisCompanyViewService.GetOrgThisCompany();
        //Header.TenantGkey = Company.TenantGkey;
        //Header.GstLocSeller = Company.GstCode;
    }

    private void SetMetalPrice()
    {
        var metalPrice = getBilledPrice("GOLD");
        if (metalPrice < 1)
        {
            displayRateErrorMsg();
            //return;
        }

        todaysRate = (decimal)metalPrice;
    }

    private decimal getBilledPrice(string metal)
    {
        var metalPrice = _settingsPageViewModel.GetPrice(metal);

        if (metalPrice is null)
        {
            metalPrice = -1;
        }

        return (decimal)metalPrice;
    }

/*    private async void SetMasterLedger()
    {
        MtblLedger = await _mtblLedgersService.GetLedger(1000);   //pass account code
    }*/

    private async void PopulateProductCategoryList()
    {
        var list = await _productCategoryService.GetProductCategoryList();
        if (list is null)
        {
            return;
        }

        ProductCategoryList = new(list
                                .Where(x => !x.Name.StartsWith("OLD"))
                                .Select(x => x.Name));
    }


    private async Task LoadReferencesAsync()
    {

        //CustOrdStatusList = await _referenceLoader.LoadValuesAsync("CUST_ORD_STATUS");

        //StateReferencesList = await _referenceLoader.LoadValuesAsync("CUST_STATE");

       // PaymentModeList = await _referenceLoader.LoadValuesAsync("PAYMENT_MODE");

        // SalesPersonReferencesList = await _referenceLoader.LoadValuesAsync("SALES_PERSON");

    }

    private async void PopulateTaxList()
    {

        var gstTaxRefList = await _mtblReferencesService.GetReferenceList("GST");
        if (gstTaxRefList is not null)
        {
            _gstTaxRefList = new(gstTaxRefList);
        }
    }

    /*    private decimal GetGstTaxRate()
        {
            var taxRate = GstTaxList.FirstOrDefault(x => x.RefCode.Equals("SGST"));
        }*/

    private async void PopulateMetalList()
    {
        var metalRefList = await _mtblReferencesService.GetReferenceList("OLD_METALS");
        MetalList = new(metalRefList.Select(x => x.RefValue));
    }

    private async void PopulateMtblRefNameList()
    {
        var mtblRefList = await _mtblReferencesService.GetReferenceList("PAYMENT_MODE");
        MtblReferencesList = new(mtblRefList);
    }

    private void SetHeader()
    {
        OmHeader = new()
        {
            TransDate = DateTime.Now,
            //IsTaxApplicable = true,
            //     GstLocSeller = Company.GstCode,
            //    TenantGkey = Company.TenantGkey
        };
    }

    private void displayRateErrorMsg()
    {
        _messageBoxService.ShowMessage($"Todays Rate not entered in system, set the rate and start invoicing....",
                                        "Todays Rate not found", MessageButton.OK, MessageIcon.Error);

    }

    [RelayCommand]
    private async Task FetchCustomerDetails(EditValueChangedEventArgs args)
    {
        if (args.NewValue is not string phoneNumber) return;

        phoneNumber = phoneNumber.Trim();

        if (string.IsNullOrEmpty(phoneNumber) || phoneNumber.Length < 10)
            return;

        if (Buyer is not null && Buyer.MobileNbr == phoneNumber)
            return;

        //CustomerReadOnly = false;
        createCustomer = false;

        Messenger.Default.Send(MessageType.WaitIndicator, WaitIndicatorVM.ShowIndicator("Fetching Customer details..."));

        Buyer = await _customerService.GetCustomer(phoneNumber);

        Messenger.Default.Send(MessageType.WaitIndicator, WaitIndicatorVM.HideIndicator());

        if (Buyer is null)
        {
            _messageBoxService.ShowMessage("No customer details found.", "Customer not found", MessageButton.OK);

            Buyer = new();
            Buyer.MobileNbr = phoneNumber;
            Buyer.Address.GstStateCode = Company.GstCode;
            Buyer.Address.State = Company.State;
            Buyer.Address.District = Company.District;

            createCustomer = true;
            //CustomerState = StateReferencesList.FirstOrDefault(x => x.RefCode == Company.GstCode);
            CustomerState = await _referenceLoader.GetValueAsync("CUST_STATE", Company.GstCode);

            Messenger.Default.Send("CustomerNameUI", MessageType.FocusTextEdit);
        }
        else
        {
            var gstCode = Buyer.Address is null ? Company.GstCode : Buyer.Address.GstStateCode;

            if (Buyer.Address is null)
            {
                Buyer.Address = new();
                Buyer.Address.GstStateCode = Company.GstCode;
            }

            //CustomerState = StateReferencesList.FirstOrDefault(x => x.RefCode == gstCode);
            CustomerState = await _referenceLoader.GetValueAsync("CUST_STATE", gstCode);

           // customerCreditCheck(Buyer);

            Messenger.Default.Send("ProductIdUIName", MessageType.FocusTextEdit);

        }

        OmHeader.CustMobile = phoneNumber;
    }

    [RelayCommand]
    private void Focus(TextEdit sender)
    {
        sender.Focus();
    }

    [RelayCommand]
    private async Task FetchProduct()
    {
        if (string.IsNullOrEmpty(ProductIdUI)) return;

        //var waitVM = WaitIndicatorVM.ShowIndicator("Fetching product details...");

        //SplashScreenManager.CreateWaitIndicator(waitVM).Show();

        //var product = await _productViewService.GetProduct(ProductIdUI);

        // await Task.Delay(30000);

        //SplashScreenManager.ActiveSplashScreens.FirstOrDefault(x => x.ViewModel == waitVM).Close();

        //this should be set as summary stock to avoid confusion
        var productStk = await _productViewService.GetProduct(ProductIdUI);

        if (productStk is null)
        {
            _messageBoxService.ShowMessage($"No Product found for {ProductIdUI}, Please make sure it exists",
                "Product not found", MessageButton.OK, MessageIcon.Error);
            return;
        }

        var metalPrice = getBilledPrice(productStk.Metal);
        if (metalPrice < 1)
        {
            displayRateErrorMsg();
            return;
        }

        OldMetalTransaction oldMetalTransaction = new OldMetalTransaction()
        {
            //Qty = 1,
            TransactedRate = metalPrice,
        };

        //invoiceLine.SetProductDetails(productStk);

/*        if (ProductSkuStock is not null)
        {
            invoiceLine.ProductSku = ProductSkuStock.ProductSku;
            invoiceLine.ProdQty = ProductSkuStock.StockQty;
            invoiceLine.ProdGrossWeight = ProductSkuStock.GrossWeight;
            invoiceLine.ProdStoneWeight = ProductSkuStock.StoneWeight;
            invoiceLine.ProdNetWeight = ProductSkuStock.NetWeight;

        }
*/
        //EvaluateFormula(invoiceLine, isInit: true);

        //TODO OmHeader.Add(omMetalTrans);

        ProductIdUI = string.Empty;

       //TODO EvaluateHeader();

        /*        if (invoiceLine.ProdGrossWeight > 0)
                {
                } else
                {
                    _messageBoxService.ShowMessage("Gross Weight cannot be zero ....",
                        "Gross Weight", MessageButton.OK, MessageIcon.Error);
                    return;
                }*/
    }

    [RelayCommand]
    private Task EvaluateOldMetalTransaction()
    {

        //   var billedPrice = _settingsPageViewModel.GetPrice(product.Metal);

        OldMetalTransaction oldMetalTransactionLine = new OldMetalTransaction()
        {
            CustGkey = OmHeader.CustGkey,
            CustMobile = OmHeader.CustMobile,
            //  TransType = "OG Purchase",
            TransDate = DateTime.Now,
            //   Uom = "Grams"
        };

        // OmHeader.ad
        // OmHeader.Add(oldMetalTransactionLine);
        return Task.CompletedTask;
    }

    [RelayCommand]
    private async Task EvaluateOldMetalTransactionLineAsync(OldMetalTransaction oldMetalTransaction)
    {

        if (string.IsNullOrEmpty(oldMetalTransaction.Metal)) return;

        OldMetalProduct = await _productViewService.GetProduct(oldMetalTransaction.Metal);

        if (OldMetalProduct is null)
        {
            _messageBoxService.ShowMessage($"No Product found for {OldMetalProduct}, Please make sure it exists",
                "Product not found", MessageButton.OK, MessageIcon.Error);
            return;
        }

        var metalPrice = _settingsPageViewModel.GetPrice(OldMetalProduct.Metal);

        if (metalPrice < 1)
        {
            displayRateErrorMsg();
            //return;
        }


        if (oldMetalTransaction.TransactedRate.GetValueOrDefault() < 1)
            oldMetalTransaction.TransactedRate = metalPrice; // todaysRate;

        oldMetalTransaction.Purity = OldMetalProduct.Purity;

        oldMetalTransaction.NetWeight = (
                                           oldMetalTransaction.GrossWeight.GetValueOrDefault() -
                                           oldMetalTransaction.StoneWeight.GetValueOrDefault() -
                                           oldMetalTransaction.WastageWeight.GetValueOrDefault()
                                        );

        oldMetalTransaction.TotalProposedPrice = oldMetalTransaction.NetWeight.GetValueOrDefault() *
                                                    oldMetalTransaction.TransactedRate.GetValueOrDefault();
        oldMetalTransaction.FinalPurchasePrice = oldMetalTransaction.TotalProposedPrice;

        oldMetalTransaction.DocRefType = "Invoice";

    }

    private async Task ProductStockSummaryUpdate(InvoiceLine line)
    {
        ProductTransaction productTransaction = new();

        var productSumryStk = await _productStockSummaryService.GetByProductGkey(line.ProductGkey);

        if (productSumryStk is not null)
        {
            //Set Product Transaction
            productTransaction.OpeningGrossWeight = productSumryStk.GrossWeight.GetValueOrDefault();
            productTransaction.OpeningStoneWeight = productSumryStk.StoneWeight.GetValueOrDefault();
            productTransaction.OpeningNetWeight = productSumryStk.NetWeight.GetValueOrDefault();

            productTransaction.ObQty = productSumryStk.StockQty.GetValueOrDefault();

            productTransaction.ProductSku = line.ProductSku;
            productTransaction.RefGkey = line.GKey;
            productTransaction.TransactionDate = DateTime.Now;
            productTransaction.ProductCategory = line.ProdCategory;

            productTransaction.TransactionType = "Issue";
            productTransaction.DocumentNbr = line.InvoiceId;
            productTransaction.DocumentDate = DateTime.Now;
            productTransaction.DocumentType = "Sales Invoice";
            productTransaction.VoucherType = "Sales";
            productTransaction.TransactionQty = line.ProdQty.GetValueOrDefault();
            productTransaction.CbQty = productSumryStk.StockQty.GetValueOrDefault() - line.ProdQty.GetValueOrDefault();

            productTransaction.TransactionGrossWeight = line.ProdGrossWeight.GetValueOrDefault();
            productTransaction.TransactionStoneWeight = line.ProdStoneWeight.GetValueOrDefault();
            productTransaction.TransactionNetWeight = line.ProdNetWeight.GetValueOrDefault();

            productTransaction.ClosingGrossWeight = productSumryStk.GrossWeight.GetValueOrDefault()
                                                            - line.ProdGrossWeight.GetValueOrDefault();
            productTransaction.ClosingStoneWeight = productSumryStk.StoneWeight.GetValueOrDefault()
                                                            - line.ProdStoneWeight.GetValueOrDefault();
            productTransaction.ClosingNetWeight = productSumryStk.NetWeight.GetValueOrDefault()
                                                            - line.ProdNetWeight.GetValueOrDefault();

            //Set Product Stock Summary
            productSumryStk.GrossWeight = (productSumryStk.GrossWeight ?? 0) - line.ProdGrossWeight;
            productSumryStk.StoneWeight = (productSumryStk.StoneWeight ?? 0) - line.ProdStoneWeight;
            productSumryStk.NetWeight = (productSumryStk.NetWeight ?? 0) - line.ProdNetWeight;
            productSumryStk.SuppliedGrossWeight = 0; //need to work ntw-- (productSumryStk.SuppliedGrossWeight ?? 0) - line.ProdGrossWeight;
            //productSumryStk.AdjustedWeight = (productSumryStk.AdjustedWeight ?? 0);
            productSumryStk.SoldWeight = 0; //ntw (productSumryStk.SoldWeight ?? 0) + line.ProdNetWeight;
            productSumryStk.BalanceWeight = (productSumryStk.BalanceWeight ?? 0) - line.ProdNetWeight;
            //productSumryStk.SuppliedQty = (productSumryStk.SuppliedQty ?? 0) + x.SuppliedQty;
            productSumryStk.SoldQty = 0; //ntw (productSumryStk.SoldQty ?? 0) + line.ProdQty;
            productSumryStk.StockQty = (productSumryStk.StockQty ?? 0) - line.ProdQty;
            //productSumryStk.AdjustedQty = (productSumryStk.AdjustedQty ?? 0);

            await _productStockSummaryService.UpdateProductStockSummary(productSumryStk);

            //productTransaction = await _productTransactionService.CreateProductTransaction(productTransaction);

            //await CreateProductTransaction(line, productSumryStk);
        }
    }

    [RelayCommand]  //(CanExecute = nameof(CanCreatePurchase))]
    private async Task CreatePurchase()
    {
        //LedgerHelper ledgerHelper = new(_ledgerService, _messageBoxService, _mtblLedgersService);   //is this a right way????? 

        //validate to fit to save invoice
/*   TODO     if (!PurchaseLineChk)
        {
            _messageBoxService.ShowMessage("Please enter Invoice details and then Save, ", "Missing Invoice Details", MessageButton.OK, MessageIcon.Error);

            return;
        }*/

/*        if (!PayRctChk)
        {
            _messageBoxService.ShowMessage("No Customer Payment details entered....", "Missing Customer Payment Details", MessageButton.OK, MessageIcon.Error);

            return;
        }*/

        //invBalanceChk = true;  //is this a right place to fix
        //var isSuccess = ProcessInvBalance();

        //if (!isSuccess) return;

        if (!string.IsNullOrEmpty(OmHeader.TransNbr))
        {
            var result = _messageBoxService.ShowMessage("Purchase nbr already exists, Do you want to print preview the Old Purchase ?", "Old Purchase",
                                                            MessageButton.OKCancel,
                                                            MessageIcon.Question,
                                                            MessageResult.Cancel);

            if (result == MessageResult.OK)
            {
               //TODO  PrintPreviewOMPurchase();
            }
            return;
        }

        if (Buyer is null || string.IsNullOrEmpty(Buyer.CustomerName))
        {
            _messageBoxService.ShowMessage("Customer information is not provided", "Customer info",
                                                MessageButton.OK, MessageIcon.Hand);
            return;
        }

        if (createCustomer)
        {
            Buyer = await _customerService.CreateCustomer(Buyer);
        }

        //Header.InvNbr = InvoiceNumberGenerator.Generate();
        OmHeader.CustGkey = (int?)Buyer.GKey;

        // this loop required? - repetition???
/*        Header.Lines.ForEach(x =>
        {
            x.InvLineNbr = Header.Lines.IndexOf(x) + 1;
            x.InvoiceId = Header.InvNbr;
        });*/

        var header = await _oldMetalTransactionService.CreateOldMetalTransaction(OmHeader);

        if (header is not null)
        {
            OmHeader.GKey = header.GKey;
            OmHeader.TransNbr = header.TransNbr;

/*            Header.Lines.ForEach(x =>
            {
                x.InvoiceHdrGkey = header.GKey;
                x.InvoiceId = header.InvNbr;
                x.TenantGkey = header.TenantGkey;
            });*/

            // loop for validation check for customer
            //await _invoiceService.CreateInvoiceLine(Header.Lines);

            //await ProcessProductTransaction(Header.Lines);

           // await ProcessOldMetalTransaction();

            _messageBoxService.ShowMessage("Invoice " + OmHeader.TransNbr + " Created Successfully", "Invoice Created",
                                                MessageButton.OK, MessageIcon.Exclamation);

            Messenger.Default.Send(MessageType.WaitIndicator, WaitIndicatorVM.ShowIndicator("Print Invoice..."));

            var waitVM = WaitIndicatorVM.ShowIndicator("Please wait.... preparing print document.... .");

            SplashScreenManager.CreateWaitIndicator(waitVM).Show();

            //TO DO PrintPreviewOMPurchase();

            SplashScreenManager.ActiveSplashScreens.FirstOrDefault(x => x.ViewModel == waitVM).Close();

        //TODO    PrintPreviewPurcahseCommand.NotifyCanExecuteChanged();
        //TODO    PrintPurchaseCommand.NotifyCanExecuteChanged();
            Messenger.Default.Send(MessageType.WaitIndicator, WaitIndicatorVM.HideIndicator());

        }
    }
}
