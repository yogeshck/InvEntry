using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DevExpress.Mvvm;
using DevExpress.Mvvm.Native;
using DevExpress.Xpf.Core;
using DevExpress.Xpf.Editors;
using DevExpress.Xpf.Grid;
using DevExpress.Xpf.Printing;
using InvEntry.Extension;
using InvEntry.Helper;
using InvEntry.Models;
using InvEntry.Models.Extensions;
using InvEntry.Reports;
using InvEntry.Services;
using InvEntry.Store;
using InvEntry.Utils;
using InvEntry.Utils.Options;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using IDialogService = DevExpress.Mvvm.IDialogService;

namespace InvEntry.ViewModels;

public partial class EstimateViewModel: ObservableObject
{
    [ObservableProperty]
    private string _customerPhoneNumber;

    [ObservableProperty]
    private MtblReference _customerState;

    [ObservableProperty]
    private MtblReference _salesPerson;

    [ObservableProperty]
    private Customer _buyer;

    [ObservableProperty]
    private OrgThisCompanyView _company;

    [ObservableProperty]
    private InvoiceArReceipt _invoiceArReceipt;

    [ObservableProperty]
    private EstimateHeader _header;

    [ObservableProperty]
    private string _productIdUI;

    [ObservableProperty]
    private MtblLedger _mtblLedger;

    [ObservableProperty]
    private string _productSku;

    [ObservableProperty]
    private string _oldMetalIdUI;

    [ObservableProperty]
    public bool _customerReadOnly;

    [ObservableProperty]
    public bool _isRefund;

    [ObservableProperty]
    public bool _isBalance;

    [ObservableProperty]
    public bool _isStockTransfer;

    [ObservableProperty]
    public bool _estimateWithTax;

    [ObservableProperty]
    private ObservableCollection<EstimateLine> selectedRows;

    [ObservableProperty]
    private ObservableCollection<string> productCategoryList;

    [ObservableProperty]
    private ObservableCollection<string> metalList;

    [ObservableProperty]
    private ObservableCollection<string> stkTrfrList;

    [ObservableProperty]
    private ObservableCollection<MtblReference> mtblReferencesList;

    [ObservableProperty]
    private ObservableCollection<MtblReference> salesPersonReferencesList;

    [ObservableProperty]
    private ObservableCollection<MtblReference> stateReferencesList;

    public ICommand ShowWindowCommand { get; set; }

    [ObservableProperty]
    private DateSearchOption _searchOption;

    private List<MtblReference> _gstTaxRefList;

    private bool createCustomer  = false;
    private bool estBalanceChk   = false;
    private bool estimateWithTax = false;
    //private bool isStockTransfer = false;

    private readonly ICustomerService _customerService;
    private readonly IProductViewService _productViewService;
    private readonly IProductStockService _productStockService;

    private readonly IDialogService _dialogService;
    private readonly IDialogService _reportDialogService;
    private readonly IMessageBoxService _messageBoxService;
    private readonly IVoucherService _voucherService;
    private readonly IEstimateService _estimateService;
    private readonly ILedgerService _ledgerService;
    private readonly IProductCategoryService _productCategoryService;
    private readonly IMtblReferencesService _mtblReferencesService;
    private readonly IOrgThisCompanyViewService _orgThisCompanyViewService;
    private readonly IProductStockSummaryService _productStockSummaryService;
    private readonly IProductTransactionService _productTransactionService;
    private readonly IInvoiceArReceiptService _invoiceArReceiptService;
    private readonly IOldMetalTransactionService _oldMetalTransactionService;
    private readonly IMtblLedgersService _mtblLedgersService;
    private readonly IReportFactoryService _reportFactoryService;
    private SettingsPageViewModel _settingsPageViewModel;
    private Dictionary<string, Action<EstimateLine, decimal?>> copyEstimateExpression;
    private Dictionary<string, Action<EstimateHeader, decimal?>> copyHeaderExpression;
    private decimal IGSTPercent = 0M;
    private decimal SCGSTPercent = 3M;

    private List<string> IGNORE_UPDATE = new List<string>
    {
        nameof(EstimateLine.VaAmount)
    };

    public EstimateViewModel(ICustomerService customerService,
        IProductViewService productViewService,
        IProductStockService productStockService,
        IDialogService dialogService,
        IEstimateService estimateService,
        IProductCategoryService productCategoryService,
        ILedgerService ledgerService,
        IProductStockSummaryService productStockSummaryService,
        IProductTransactionService productTransactionService,
        IMessageBoxService messageBoxService,
        IMtblReferencesService mtblReferencesService,
        IOrgThisCompanyViewService orgThisCompanyViewService,
        IVoucherService voucherService,
        IInvoiceArReceiptService invoiceArReceiptService,
        IOldMetalTransactionService oldMetalTransactionService,
        IMtblLedgersService mtblLedgersService,
        SettingsPageViewModel settingsPageViewModel,
        IReportFactoryService reportFactoryService,
        [FromKeyedServices("ReportDialogService")] IDialogService reportDialogService)
    {

        //MtblRefNameList = new();


        
        _estimateService = estimateService;
        _orgThisCompanyViewService = orgThisCompanyViewService;
        _customerService = customerService;
        _productViewService = productViewService;
        _productStockService = productStockService;
        _productStockSummaryService = productStockSummaryService;
        _productTransactionService = productTransactionService;
        _productCategoryService = productCategoryService;
        _dialogService = dialogService;
        _ledgerService = ledgerService;
        _messageBoxService = messageBoxService;
        _mtblLedgersService = mtblLedgersService;
        _reportDialogService = reportDialogService;
        _reportFactoryService = reportFactoryService;
        _oldMetalTransactionService = oldMetalTransactionService;
        _voucherService = voucherService;
        _invoiceArReceiptService = invoiceArReceiptService;
        _mtblReferencesService = mtblReferencesService;

        selectedRows = new();
        _customerReadOnly = false;

        _isBalance = true;
        _isRefund = false;
        _isStockTransfer = true;
        _settingsPageViewModel = settingsPageViewModel;

        var metalPrice = getBilledPrice("GOLD");
        if (metalPrice < 1)
        {
            displayErrorMsg();
            //return;
        }

        SetHeader();
        SetThisCompany();
        SetMasterLedger();

        //SetThisCompany();

        PopulateProductCategoryList();
        PopulateStateList();
        PopulateUnboundLineDataMap();
        PopulateMtblRefNameList();
        PopulateMetalList();
        PopulateStockTransfer();

        PopulateTaxList();
        PopulateSalesPersonList();

        //PopulateUnboundHeaderDataMap();
    }

    private async void SetMasterLedger()
    {
        MtblLedger = await _mtblLedgersService.GetLedger(1000);   //pass account code
    }

    private async void PopulateProductCategoryList()
    {
        var list = await _productCategoryService.GetProductCategoryList();
        ProductCategoryList = new(list.Select(x => x.Name));
    }

    private async void PopulateSalesPersonList()
    {
        var salesPersonRefList = await _mtblReferencesService.GetReferenceList("SALES_PERSON");

        if (salesPersonRefList is null)
        {
            SalesPersonReferencesList = new();
            SalesPersonReferencesList.Add(new MtblReference() { RefValue = "Ebi", RefCode = "33" }); //Yet To fix
            SalesPersonReferencesList.Add(new MtblReference() { RefValue = "Vinnila", RefCode = "32" });
            SalesPersonReferencesList.Add(new MtblReference() { RefValue = "Anju", RefCode = "30" });
            return;
        }

        SalesPersonReferencesList = new(salesPersonRefList);

    }

    private async void PopulateStateList()
    {
        var stateRefList = await _mtblReferencesService.GetReferenceList("CUST_STATE");

        if (stateRefList is null)
        {
            StateReferencesList = new();
            StateReferencesList.Add(new MtblReference() { RefValue = "Tamil Nadu", RefCode = "33" });
            StateReferencesList.Add(new MtblReference() { RefValue = "Kerala", RefCode = "32" });
            StateReferencesList.Add(new MtblReference() { RefValue = "Karnataka", RefCode = "30" });
            return;
        }

        StateReferencesList = new(stateRefList);

    }

    private async void PopulateTaxList()
    {

        var gstTaxRefList = await _mtblReferencesService.GetReferenceList("GST");
        if (gstTaxRefList is not null)
        {
            _gstTaxRefList = new(gstTaxRefList);
        }
    }

    private async void PopulateMetalList()
    {
        var metalRefList = await _mtblReferencesService.GetReferenceList("OLD_METALS");
        MetalList = new(metalRefList.Select(x => x.RefValue));
    }

    private async void PopulateStockTransfer()
    {
        var stkTrfrRefList = await _mtblReferencesService.GetReferenceList("STOCK_TRANSFER");
        StkTrfrList = new(stkTrfrRefList.Select(x => x.RefValue));
    }

    private async void PopulateMtblRefNameList()
    {
        var mtblRefList = await _mtblReferencesService.GetReferenceList("PAYMENT_MODE");
        MtblReferencesList = new(mtblRefList);
    }

    private async void SetThisCompany()
    {
        Company = new();
        Company = await _orgThisCompanyViewService.GetOrgThisCompany();
        //Header.TenantGkey = Company.TenantGkey;
        Header.GstLocSeller = Company.GstCode;
    }

    private void PopulateUnboundLineDataMap()
    {
        if (copyEstimateExpression is null) copyEstimateExpression = new();

        copyEstimateExpression.Add($"{nameof(EstimateLine.EstlTaxableAmount)}", (item, val) => item.EstlTaxableAmount = val);
        copyEstimateExpression.Add($"{nameof(EstimateLine.ProdNetWeight)}", (item, val) => item.ProdNetWeight = val);
        copyEstimateExpression.Add($"{nameof(EstimateLine.EstlGrossAmt)}", (item, val) => 
                                    item.EstlGrossAmt = val * (item.Metal.Equals("DIAMOND") ? 100 : 1));
        copyEstimateExpression.Add($"{nameof(EstimateLine.VaAmount)}", (item, val) => item.VaAmount = val);
        copyEstimateExpression.Add($"{nameof(EstimateLine.EstlCgstAmount)}", (item, val) => item.EstlCgstAmount = val);
        copyEstimateExpression.Add($"{nameof(EstimateLine.EstlSgstAmount)}", (item, val) => item.EstlSgstAmount = val);
        copyEstimateExpression.Add($"{nameof(EstimateLine.EstlIgstAmount)}", (item, val) => item.EstlIgstAmount = val);
        copyEstimateExpression.Add($"{nameof(EstimateLine.EstlTotal)}", (item, val) => item.EstlTotal = val);
    }

    private void PopulateUnboundHeaderDataMap()
    {
        if (copyHeaderExpression is null) copyHeaderExpression = new();

        copyHeaderExpression.Add($"{nameof(EstimateHeader.RoundOff)}", (item, val) => item.RoundOff = val);
        copyHeaderExpression.Add($"{nameof(EstimateHeader.GrossRcbAmount)}", (item, val) => item.GrossRcbAmount = val);
        copyHeaderExpression.Add($"{nameof(EstimateHeader.AmountPayable)}", (item, val) => item.AmountPayable = val);
        copyHeaderExpression.Add($"{nameof(EstimateHeader.EstBalance)}", (item, val) => item.EstBalance = val);
    }

    partial void OnCustomerStateChanged(MtblReference value)
    {
/*        if (Buyer is null) return;

        Buyer.GstStateCode = value.RefCode;    //Need to fetch based on pincode - future change
        Header.GstLocBuyer = value.RefCode;
        Header.CgstPercent = GetGSTWithinState();
        Header.SgstPercent = GetGSTWithinState();
        Header.IgstPercent = IGSTPercent;*/

            if (Buyer is null) return;

            Buyer.GstStateCode = value.RefCode;

            Header.CgstPercent = GetGSTPercent("CGST");
            Header.SgstPercent = GetGSTPercent("SGST");
            Header.IgstPercent = GetGSTPercent("IGST");

            //Need to fetch based on pincode - future change
            Header.GstLocBuyer = value.RefCode;

            EvaluateForAllLines();
            EvaluateHeader();
        
    }

    partial void OnSalesPersonChanged(MtblReference value)
    {
        if (Buyer is null) return;

        if (value.RefValue is not null)
        {
          //  Header.SalesPerson = value.RefValue;
        }
    }


    [RelayCommand]
    private void AddReceipts()
    {
        var vm = new InvoiceReceiptsViewModel();
        var result = _dialogService.ShowDialog(MessageButton.OKCancel, "Receipts", "InvoiceReceiptsView", vm);

        if (result == MessageResult.OK)
        {

        }
        else
        {

        }
    }

    [RelayCommand]
    private async Task FetchCustomer(EditValueChangedEventArgs args)
    {
        if (args.NewValue is not string phoneNumber) return;

        phoneNumber = phoneNumber.Trim();

        if (string.IsNullOrEmpty(phoneNumber) || phoneNumber.Length < 10)
            return;

        if (Buyer is not null && Buyer.MobileNbr == phoneNumber)
            return;

        CustomerReadOnly = false;
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
            CustomerState = StateReferencesList.FirstOrDefault(x => x.RefCode == Company.GstCode);


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

            CustomerState = StateReferencesList.FirstOrDefault(x => x.RefCode == gstCode);

            Messenger.Default.Send("ProductIdUIName", MessageType.FocusTextEdit);


/*            if (EstimateWithTax)
                IGSTPercent = Buyer.GstStateCode == "33" ? 0M : 3M;

            Header.CgstPercent = GetGSTWithinState();
            Header.SgstPercent = GetGSTWithinState();
            Header.IgstPercent = IGSTPercent;*/
        }

        Header.CustMobile = phoneNumber;

        //to effect stock update though it is just estimate - being used for stock transfer to other branches
      //  foreach (var item in StkTrfrList)
      //  {
      //      if (item is not null && item == phoneNumber)
      //          IsStockTransfer = true;
      //  }

    }

    [RelayCommand]
    private async Task FetchProduct()
    {
        if (string.IsNullOrEmpty(ProductIdUI)) return;

        var waitVM = WaitIndicatorVM.ShowIndicator("Fetching product details...");

        SplashScreenManager.CreateWaitIndicator(waitVM).Show();

        var product = await _productViewService.GetProduct(ProductIdUI);

        // await Task.Delay(30000);

        SplashScreenManager.ActiveSplashScreens.FirstOrDefault(x => x.ViewModel == waitVM).Close();

        if (product is null)
        {
            _messageBoxService.ShowMessage($"No Product found for {ProductIdUI}, Please make sure it exists",
                "Product not found", MessageButton.OK, MessageIcon.Error);
            return;
        }

        var metalPrice = _settingsPageViewModel.GetPrice(product.Metal);

        if (metalPrice < 1)
        {
            displayErrorMsg();
            return;
        }

        EstimateLine estimateLine = new EstimateLine()
        {
            ProdQty = 1,
            EstlBilledPrice = metalPrice,
            EstlCgstPercent = Header.CgstPercent,
            EstlSgstPercent = Header.SgstPercent,
            EstlIgstPercent = Header.IgstPercent,
            EstlStoneAmount = 0M,
            TaxType = "GST"

        };


        estimateLine.SetProductDetails(product);   

        EvaluateFormula(estimateLine, isInit: true);

        Header.Lines.Add(estimateLine);

        ProductIdUI = string.Empty;

        EvaluateHeader();

        /*        if (invoiceLine.ProdGrossWeight > 0)
                {
                } else
                {
                    _messageBoxService.ShowMessage("Gross Weight cannot be zero ....",
                        "Gross Weight", MessageButton.OK, MessageIcon.Error);
                    return;
                }*/
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

    private void displayErrorMsg()
    {
        _messageBoxService.ShowMessage($"Todays Rate not entered in system, set the rate and start invoicing....",
                                        "Todays Rate not found", MessageButton.OK, MessageIcon.Error);

    }

    private bool CanProcessArReceipts()
    {
        return !Header.DiscountAmount.HasValue || Header.DiscountAmount.Value == 0;
    }

    [RelayCommand]
    private void AddOldJewel(string type)
    {
        var enumVal = Enum.Parse<MetalType>(type);

        var vm = new DialogOldJewelVM();

        if (_dialogService.ShowDialog(MessageButton.OKCancel, "Exchange", "DialogOldJewel", vm) == MessageResult.OK)
        {
            if (enumVal == MetalType.Gold)
            {
                Header.OldGoldAmount = vm.Rate * vm.Weight;
            }
            else
            {
                Header.OldSilverAmount = vm.Rate * vm.Weight;
            }
        }
    }

    [RelayCommand(CanExecute = nameof(CanCreateEstimate))]
    private async Task CreateEstimate()
    {
        LedgerHelper ledgerHelper = new(_ledgerService, _messageBoxService, _mtblLedgersService);   //is this a right way????? 

        estBalanceChk = true;  //is this a right place to fix
        var isSuccess = ProcessEstBalance();

        if (!isSuccess) return;

        if (!string.IsNullOrEmpty(Header.EstNbr))
        {
            var result = _messageBoxService.ShowMessage("Estimate already created, Do you want to print preview the Estimate ?", "Estoice", MessageButton.OKCancel, MessageIcon.Question, MessageResult.Cancel);

            if (result == MessageResult.OK)
            {
                PrintPreviewEstimate();
            }
            return;
        }

        if (Buyer is null || string.IsNullOrEmpty(Buyer.CustomerName))
        {
            _messageBoxService.ShowMessage("Customer information is not provided", "Customer info", MessageButton.OK, MessageIcon.Hand);
            return;
        }

        if (createCustomer)
        {
            Buyer = await _customerService.CreateCustomer(Buyer);
        }

        //Header.InvNbr = InvoiceNumberGenerator.Generate();
        Header.CustGkey = (int?)Buyer.GKey;

        Header.Lines.ForEach(x =>
        {
            x.EstLineNbr = Header.Lines.IndexOf(x) + 1;
            x.EstimateId = Header.EstNbr;
            x.EstimateHdrGkey = Header.GKey;
        });

        var header = await _estimateService.CreateHeader(Header);

        if (header is not null)
        {
            Header.GKey = header.GKey;
            Header.EstNbr = header.EstNbr;
            Header.Lines.ForEach(x =>
            {
                x.EstimateHdrGkey = header.GKey;
                x.EstimateId = header.EstNbr;
                x.EstimateHdrGkey = Header.GKey;
                x.TenantGkey = header.TenantGkey;
            });
            // loop for validation check for customer
            await _estimateService.CreateEstimateLine(Header.Lines);

            if (IsStockTransfer)
                await ProcessProductTransaction(Header.Lines);

            await ProcessOldMetalTransaction();

            //Invoice header details needs to be saved alongwith receipts, hence calling from here.
            ProcessReceipts();

            // to check the logic
        //    if ((Header.AdvanceAdj > 0) || (Header.RdAmountAdj > 0))
        //        await ledgerHelper.ProcessInvoiceAdvanceAsync(Header);

            _messageBoxService.ShowMessage("Estimate Created Successfully", "Estimate Created", MessageButton.OK, MessageIcon.Exclamation);

            Messenger.Default.Send(MessageType.WaitIndicator, WaitIndicatorVM.ShowIndicator("Print Estimate..."));
            PrintPreviewEstimate();
            PrintPreviewEstimateCommand.NotifyCanExecuteChanged();
            PrintEstimateCommand.NotifyCanExecuteChanged();
            Messenger.Default.Send(MessageType.WaitIndicator, WaitIndicatorVM.HideIndicator());

        }
    }

    private async Task ProcessProductTransaction(IEnumerable<EstimateLine> estLines)
    {
        foreach (var line in estLines)
        {

            await ProductStockSummaryUpdate(line);

            //await ProductStockUpdate(line);  //Put in on-hold for time being - till we introduce barcode

            //await CreateProductTransaction(line);
        }
    }

/*    private async Task ProductStockUpdate(EstimateLine line)
    {

        var productStk = await _productStockService.GetProductStock(line.ProductSku);

        productStk.SoldWeight = line.ProdGrossWeight;
        productStk.BalanceWeight = 0;
        productStk.SoldQty = line.ProdQty;
        productStk.StockQty = 0;
        productStk.Status = "Sold";
        productStk.IsProductSold = true;

        await _productStockService.UpdateProductStock(productStk);

    }*/

    //Consolidated stock line in product stock summary - stock qty / weight has to be reduced based on invoiced qty
    //this logic would be revisited/changed once product sku - barcode feature introduced
    //WARN :- Ensure all product category must have one record in table
    private async Task ProductStockSummaryUpdate(EstimateLine estline)
    {
        ProductTransaction productTransaction = new();

        var productSumryStk = await _productStockSummaryService.GetByProductGkey(estline.ProductGkey);

        if (productSumryStk is null) 
        {
            return; 
        }
        else if (productSumryStk is not null)
        {
            //Set Product Transaction
            productTransaction.OpeningGrossWeight = productSumryStk.GrossWeight.GetValueOrDefault();
            productTransaction.OpeningStoneWeight = productSumryStk.StoneWeight.GetValueOrDefault();
            productTransaction.OpeningNetWeight = productSumryStk.NetWeight.GetValueOrDefault();

            productTransaction.ObQty = productSumryStk.StockQty.GetValueOrDefault();  //Current stock qty - before executing transaction

            //productTransaction.ProductSku = estline.ProductSku;
            productTransaction.RefGkey = estline.GKey;
            productTransaction.TransactionDate = DateTime.Now;
            productTransaction.ProductCategory = estline.ProdCategory;

            productTransaction.TransactionType = "Issue";
            productTransaction.DocumentNbr = estline.EstimateId;
            productTransaction.DocumentDate = DateTime.Now;
            productTransaction.DocumentType = "Stock Transfer";
            productTransaction.VoucherType = "Stock Transfer";
            productTransaction.TransactionQty = estline.ProdQty;
            productTransaction.CbQty = productTransaction.ObQty.GetValueOrDefault() - estline.ProdQty;

            productTransaction.TransactionGrossWeight = estline.ProdGrossWeight.GetValueOrDefault();
            productTransaction.TransactionStoneWeight = estline.ProdStoneWeight.GetValueOrDefault();
            productTransaction.TransactionNetWeight = estline.ProdNetWeight.GetValueOrDefault();

            productTransaction.ClosingGrossWeight = productTransaction.OpeningGrossWeight.GetValueOrDefault()
                                                            - estline.ProdGrossWeight.GetValueOrDefault();
            productTransaction.ClosingStoneWeight = productTransaction.OpeningStoneWeight.GetValueOrDefault()
                                                            - estline.ProdStoneWeight.GetValueOrDefault();
            productTransaction.ClosingNetWeight = productTransaction.OpeningNetWeight.GetValueOrDefault()
                                                            - estline.ProdNetWeight.GetValueOrDefault();

            //Set Product Stock Summary
            productSumryStk.GrossWeight = (productSumryStk.GrossWeight ?? 0) - estline.ProdGrossWeight;
            productSumryStk.StoneWeight = (productSumryStk.StoneWeight ?? 0) - estline.ProdStoneWeight;
            productSumryStk.NetWeight = (productSumryStk.NetWeight ?? 0) - estline.ProdNetWeight;
            productSumryStk.SuppliedGrossWeight = (productSumryStk.SuppliedGrossWeight ?? 0) - estline.ProdGrossWeight;
            //productSumryStk.AdjustedWeight = (productSumryStk.AdjustedWeight ?? 0);
            productSumryStk.SoldWeight = (productSumryStk.SoldWeight ?? 0) + estline.ProdNetWeight;
            productSumryStk.BalanceWeight = (productSumryStk.BalanceWeight ?? 0) - estline.ProdNetWeight;
            //productSumryStk.SuppliedQty = (productSumryStk.SuppliedQty ?? 0) + x.SuppliedQty;
            productSumryStk.SoldQty = (productSumryStk.SoldQty ?? 0) + estline.ProdQty;
            productSumryStk.StockQty = (productSumryStk.StockQty ?? 0) - estline.ProdQty;
            //productSumryStk.AdjustedQty = (productSumryStk.AdjustedQty ?? 0);

            await _productStockSummaryService.UpdateProductStockSummary(productSumryStk);

            productTransaction = await _productTransactionService.CreateProductTransaction(productTransaction);

            //await CreateProductTransaction(line, productSumryStk);
        }
    }

    private bool CanCreateEstimate()
    {
        return string.IsNullOrEmpty(Header?.EstNbr);
    }

    [RelayCommand(CanExecute = nameof(CanPrintEstimate))]
    private void PrintEstimate()
    {
        //after report uncomment this
            var printed = PrintHelper.Print(_reportFactoryService.CreateEstimateReport(Header.EstNbr, Header.GKey, Company ));
        
            if (printed.HasValue && printed.Value)
                _messageBoxService.ShowMessage("Estimate printed Successfully", "Estimate print", MessageButton.OK, MessageIcon.None);
    }

    private bool CanPrintEstimate()
    {
        return !CanCreateEstimate();
    }

    [RelayCommand(CanExecute = nameof(CanPrintEstimate))]
    private void PrintPreviewEstimate()
    {
        _reportDialogService.PrintPreviewEstimate(Header.EstNbr, Header.GKey, Company);
        ResetEstimate();
    }

    [RelayCommand(CanExecute = nameof(CanPrintEstimate))]
    private void ExportToPdf()
    {
     
            _reportFactoryService.CreateEstimateReportPdf(Header.EstNbr, Header.GKey, Company, "D:\\Madrone\\Invoice\\");
    }

    [RelayCommand]
    private void CellUpdate(CellValueChangedEventArgs args)
    {
        if (args.Row is EstimateLine line)
        {
            EvaluateFormula(line);
        }
        else
        if (args.Row is InvoiceArReceipt arInvRctline)
        {
            EvaluateArRctLine(arInvRctline);
        }
        else
        if (args.Row is OldMetalTransaction oldMetalTransaction &&
            args.Column.FieldName != nameof(OldMetalTransaction.FinalPurchasePrice))
        {
            EvaluateOldMetalTransactions(oldMetalTransaction);
        }

        EvaluateHeader();
    }

    [RelayCommand]
    private void EvaluateArRctLine(InvoiceArReceipt arInvRctLine)
    {

        if (string.IsNullOrEmpty(arInvRctLine.TransactionType))
        {
            return;
        }

        if (!arInvRctLine.BalBeforeAdj.HasValue)
            arInvRctLine.BalBeforeAdj = Header.EstBalance.GetValueOrDefault();

        arInvRctLine.BalanceAfterAdj = arInvRctLine.BalBeforeAdj.GetValueOrDefault() -
                                        arInvRctLine.AdjustedAmount.GetValueOrDefault();


        if (arInvRctLine.TransactionType == "Cash" || arInvRctLine.TransactionType == "Refund")
        {
            arInvRctLine.ModeOfReceipt = "Cash";
        }
        else if (arInvRctLine.TransactionType == "Credit")
        {
            arInvRctLine.ModeOfReceipt = "Credit";
        }
        else
        {
            arInvRctLine.ModeOfReceipt = "Bank";
        }

        EvaluateHeader();

    }

    [RelayCommand]
    private void EvaluateOldMetalTransactions(OldMetalTransaction oldMetalTransaction)
    {

        /*        if (oldMetalTransaction.ProductId is null)
                {
                    return;
                }*/

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

    [RelayCommand]
    private Task EvaluateOldMetalTransaction()
    {

        //   var billedPrice = _settingsPageViewModel.GetPrice(product.Metal);

        OldMetalTransaction oldMetalTransactionLine = new OldMetalTransaction()
        {
            CustGkey = Header.CustGkey,
            CustMobile = Header.CustMobile,
            //  TransType = "OG Purchase",
            TransDate = DateTime.Now,
            //   Uom = "Grams"
        };

        Header.OldMetalTransactions.Add(oldMetalTransactionLine);
        return Task.CompletedTask;
    }

    [RelayCommand(CanExecute = nameof(CanProcessArReceipts))]
    private async Task ProcessArReceipts()
    {
        //  var paymentMode = await _mtblReferencesService.GetReference("PAYMENT_MODE");
        // _productService.GetProduct(ProductIdUI);

        // var waitVM = WaitIndicatorVM.ShowIndicator("Fetching Invoice Receipt details...");

        var noOfLines = Header.ReceiptLines.Count;

        InvoiceArReceipt arInvRctLine = new InvoiceArReceipt()
        {
            CustGkey = Header.CustGkey,
            Status = "Open",    //Status Open - Before Adjustment
            SeqNbr = noOfLines + 1
        };

        Header.ReceiptLines.Add(arInvRctLine);
    }

    private decimal? FilterMetalTransactions(string metal)
    {
        return Header.OldMetalTransactions
                        .Where(x => metal.Equals(x.Metal, StringComparison.OrdinalIgnoreCase))
                        .Select(x => x.FinalPurchasePrice)
                        .Sum();
    }

    [RelayCommand]
    private void EvaluateHeader()
    {

        // Header.AdvanceAdj = FilterReceiptTransactions("Advance");
        // Header.RdAmountAdj = FilterReceiptTransactions("RD");

        Header.RecdAmount = Header.ReceiptLines.Select(x => x.AdjustedAmount).Sum();

        Header.OldGoldAmount = FilterMetalTransactions("OLD GOLD 18KT") + FilterMetalTransactions("OLD GOLD 22KT") + FilterMetalTransactions("OLD GOLD 916-22KT");

        Header.OldSilverAmount = FilterMetalTransactions("OLD SILVER");

        // TaxableTotal from line without tax value
        Header.EstlTaxTotal = Header.Lines.Select(x => x.EstlTotal).Sum();

        // Line Taxable Total minus Old Gold & Silver Amount
        decimal BeforeTax = 0;
        BeforeTax = Header.EstlTaxTotal.GetValueOrDefault() -
                    Header.OldGoldAmount.GetValueOrDefault() -
                    Header.OldSilverAmount.GetValueOrDefault();

        if (BeforeTax >= 0)
        {
            Header.CgstAmount = MathUtils.Normalize(BeforeTax * Math.Round(Header.CgstPercent.GetValueOrDefault() / 100, 3));
            Header.SgstAmount = MathUtils.Normalize(BeforeTax * Math.Round(Header.SgstPercent.GetValueOrDefault() / 100, 3));
            Header.IgstAmount = MathUtils.Normalize(BeforeTax * Math.Round(Header.IgstPercent.GetValueOrDefault() / 100, 3));

        }
        else
        {
            Header.CgstAmount = 0;
            Header.SgstAmount = 0;
            Header.IgstAmount = 0;
        }

        Header.EstlTaxableAmount = BeforeTax;

        // After Tax Gross Value
        Header.GrossRcbAmount = 0;
        Header.GrossRcbAmount = BeforeTax +
                                Header.CgstAmount.GetValueOrDefault() +
                                Header.SgstAmount.GetValueOrDefault() +
                                Header.IgstAmount.GetValueOrDefault();

        decimal roundOff = 0;
        roundOff = Math.Round(Header.GrossRcbAmount.GetValueOrDefault(), 0) -
                        Header.GrossRcbAmount.GetValueOrDefault();

        Header.RoundOff = roundOff; // Math.Round(Header.GrossRcbAmount.GetValueOrDefault(), 0);

        Header.GrossRcbAmount = MathUtils.Normalize(Header.GrossRcbAmount.GetValueOrDefault(), 0);

        decimal payableValue = 0;
        payableValue = Header.GrossRcbAmount.GetValueOrDefault() -
                        Header.DiscountAmount.GetValueOrDefault();

        Header.AmountPayable = 0;
        Header.AmountPayable = MathUtils.Normalize(payableValue);

        Header.EstBalance = MathUtils.Normalize(Header.AmountPayable.GetValueOrDefault()) -
            (
                Header.RecdAmount.GetValueOrDefault() +
                Header.AdvanceAdj.GetValueOrDefault() +
                Header.RdAmountAdj.GetValueOrDefault()
             );

        if (estBalanceChk)
        {
            ProcessEstBalance();
        }
    }

    private bool ProcessEstBalance()
    {

        ProcessSettlements();

        //Note if inv balance is greater than zero - we need to show message to get confirmation from user
        // and warn to check there is unpaid balance........ 

        if (Header.EstBalance > 0)
        {
            var result = _messageBoxService.ShowMessage("Received Amount is lesser than the Invoice Amount, " +
                "Do you want to make Credit for the balance Invoice Amount of Rs. " + Header.EstBalance + " ?", "Estimate", MessageButton.YesNo, MessageIcon.Question, MessageResult.No);

            if (result == MessageResult.Yes)
            {
                Header.PaymentDueDate = Header.EstDate.Value.AddDays(7);
                Header.EstRefund = 0M;
                BalanceVisible();
                SetReceipts("Credit");
            }
            else
            {
                return false;
            }
        }
        else if (Header.EstBalance == 0)
        {
            Header.PaymentDueDate = null;
            BalanceVisible();

        }
        else if (Header.EstBalance < 0)
        {
            var result = _messageBoxService.ShowMessage("Received Amount is more than Invoice Amount, " +
                "Do you want to Refund excess Amount of Rs. " + Header.EstBalance + " ?", "Estimate", MessageButton.YesNo, MessageIcon.Question, MessageResult.No);

            if (result == MessageResult.Yes)
            {
                Header.PaymentDueDate = null;
                Header.EstRefund = Header.EstBalance * -1;
                Header.EstBalance = 0M;
                RefundVisible();
                SetReceipts("Refund");
            }
            else
            {
                return false;
            }

        }

        return true;
    }

    private void ProcessSettlements()
    {
        if (Header.DiscountAmount > 0)
        {
            SetReceipts("Discount");
        }
        if (Header.AdvanceAdj > 0)
        {
            SetReceipts("Advance Adj");

        }
        if (Header.RdAmountAdj > 0)
        {
            SetReceipts("Recurring Deposit");
        }
    }

    private void SetReceipts(String str)
    {

        var noOfLines = Header.ReceiptLines.Count;

        InvoiceArReceipt arInvRct = new InvoiceArReceipt();

        arInvRct.TransactionType = str;
        arInvRct.ModeOfReceipt = str;
        arInvRct.SeqNbr = noOfLines + 1;
        var adjustedAmount = getTransAmount(str);
        arInvRct.AdjustedAmount = adjustedAmount;

        Header.ReceiptLines.Add(arInvRct);
    }

    private async void ProcessReceipts()
    {
        //For each Receipts row - seperate Voucher has to be created
        foreach (var receipts in Header.ReceiptLines)
        {
            if (receipts is null) return;

            var voucher = CreateVoucher(receipts);
            voucher = await SaveVoucher(voucher);

            var arReceipts = CreateArReceipts(receipts, voucher);
            await SaveArReceipts(arReceipts);

        }
    }

    private async Task ProcessOldMetalTransaction()
    {

        foreach (var omTrans in Header.OldMetalTransactions)
        {
            omTrans.EnrichEstHeaderDetails(Header);
        }

        await _oldMetalTransactionService.CreateOldMetalTransaction(Header.OldMetalTransactions);
    }

    private InvoiceArReceipt CreateArReceipts(InvoiceArReceipt invoiceArReceipt, Voucher voucher)
    {

        InvoiceArReceipt arInvRct = new()
        {
            //VoucherDate = DateTime.Now
        };

        arInvRct.SeqNbr = invoiceArReceipt.SeqNbr;
        arInvRct.CustGkey = invoiceArReceipt.CustGkey;
        arInvRct.InvoiceGkey = (int?)Header.GKey;
        arInvRct.InvoiceNbr = Header.EstNbr;
        arInvRct.InvoiceReceivableAmount = invoiceArReceipt.InvoiceReceivableAmount;
        arInvRct.BalanceAfterAdj = invoiceArReceipt.BalanceAfterAdj;
        arInvRct.TransactionType = invoiceArReceipt.TransactionType;
        arInvRct.ModeOfReceipt = invoiceArReceipt.ModeOfReceipt;
        arInvRct.BalBeforeAdj = invoiceArReceipt.BalBeforeAdj;
        arInvRct.InternalVoucherNbr = voucher.VoucherNbr;
        arInvRct.InternalVoucherDate = voucher.VoucherDate;
        arInvRct.InvoiceReceiptNbr = Header.EstNbr.Replace("B", "R");  //hard coded - future review 
        arInvRct.Status = "Adj";

        var adjustedAmount = getTransAmount(invoiceArReceipt.TransactionType);
        arInvRct.AdjustedAmount = adjustedAmount == 0 ? invoiceArReceipt.AdjustedAmount : adjustedAmount;

        return arInvRct;

    }

    private Voucher CreateVoucher(InvoiceArReceipt invoiceArReceipt)
    {

        Voucher Voucher = new()
        {
            VoucherDate = DateTime.Now
        };

        Voucher.SeqNbr = 1;
        Voucher.CustomerGkey = Header.CustGkey;
        Voucher.VoucherDate = Header.EstDate;
        Voucher.TransType = "Receipt";         // Trans_type    1 = Receipt,    2 = Payment,    3 = Journal
        Voucher.VoucherType = invoiceArReceipt.TransactionType; // Voucher_type  1 = Sales,      2 = Credit,     3 = Expense
        Voucher.Mode = invoiceArReceipt.ModeOfReceipt; // Mode          1 = Cash,       2 = Bank,       3 = Credit
        Voucher.TransDate = Voucher.VoucherDate;    // DateTime.Now;
        Voucher.VoucherNbr = Header.EstNbr;
        Voucher.RefDocNbr = Header.EstNbr;
        Voucher.RefDocDate = Header.EstDate;
        Voucher.RefDocGkey = Header.GKey;
        Voucher.TransDesc = Voucher.VoucherType + "-" + Voucher.TransType + "-" + Voucher.Mode;

        var transAmount = getTransAmount(invoiceArReceipt.TransactionType);
        Voucher.TransAmount = transAmount == 0 ? invoiceArReceipt.AdjustedAmount : transAmount;

        return Voucher;

    }

    private decimal? getTransAmount(string transType)
    {
        return transType switch
        {
            var s when s.Equals("Recurring Deposit", StringComparison.OrdinalIgnoreCase) => Header.RdAmountAdj,
            var s when s.Equals("Refund", StringComparison.OrdinalIgnoreCase) => Header.EstRefund,
            var s when s.Equals("Credit", StringComparison.OrdinalIgnoreCase) => Header.EstBalance,
            var s when s.Equals("Discount", StringComparison.OrdinalIgnoreCase) => Header.DiscountAmount,
            var s when s.Equals("Advance Adj", StringComparison.OrdinalIgnoreCase) => Header.AdvanceAdj,
            _ => 0M
        };

    }

    private async Task SaveArReceipts(InvoiceArReceipt invoiceArReceipt)
    {
        if (invoiceArReceipt.GKey == 0)
        {
            try
            {
                var voucherResult = await _invoiceArReceiptService.CreateInvArReceipt(invoiceArReceipt);

                if (voucherResult != null)
                {
                    invoiceArReceipt = voucherResult;
                }

            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }

        }
        else
        {
            await _invoiceArReceiptService.UpdateInvArReceipt(invoiceArReceipt);
        }

    }

    //[RelayCommand]
    private async Task<Voucher> SaveVoucher(Voucher voucher)
    {
        if (voucher.GKey == 0)
        {
            var voucherResult = await _voucherService.CreateVoucher(voucher);

            if (voucherResult != null)
            {
                voucher = voucherResult;
                //  _messageBoxService.ShowMessage("Voucher Created Successfully", "Voucher Created",
                //      MessageButton.OK, MessageIcon.Exclamation);
            }
        }
        else
        {
            await _voucherService.UpdateVoucher(voucher);
        }

        return voucher;

    }


    [RelayCommand]
    private void Focus(TextEdit sender)
    {
        sender.Focus();
    }

    [RelayCommand]
    private void ResetEstimate()
    {
        // var result = _messageBoxService.ShowMessage("Reset all values", "Reset Invoice", MessageButton.YesNo, MessageIcon.Question, MessageResult.No);

        // if (result == MessageResult.No)
        //     return;

        SetHeader();
        SetThisCompany();
        Buyer = null;
        CustomerPhoneNumber = null;
        CustomerState = null;
        SalesPerson = null;
        CreateEstimateCommand.NotifyCanExecuteChanged();
        estBalanceChk = false;  //reset to false for next estimate
    }

    [RelayCommand(CanExecute = nameof(CanDeleteRows))]
    private void DeleteRows()
    {
        var result = _messageBoxService.ShowMessage("Delete all selected rows", "Delete Rows", MessageButton.YesNo, MessageIcon.Question, MessageResult.No);

        if (result == MessageResult.No)
            return;

        List<int> indexs = new List<int>();
        foreach (var row in SelectedRows)
        {
            indexs.Add(Header.Lines.IndexOf(row));
        }

        indexs.ForEach(x =>
        {
            if (x >= 0)
            {
                Header.Lines.RemoveAt(x);
            }
        });

        EvaluateForAllLines();
        EvaluateHeader();
    }

    private bool CanDeleteRows()
    {
        return SelectedRows?.Any() ?? false;
    }

    [RelayCommand(CanExecute = nameof(CanDeleteSingleRow))]
    private void DeleteSingleRow(EstimateLine line)
    {
        var result = _messageBoxService.ShowMessage("Delete current row", "Delete Row", MessageButton.YesNo, MessageIcon.Question, MessageResult.No);

        if (result == MessageResult.No)
            return;

        var index = Header.Lines.Remove(line);
    }

    private bool CanDeleteSingleRow(EstimateLine line)
    {
        return line is not null && Header.Lines.IndexOf(line) > -1;
    }

    private void SetHeader()
    {
        Header = new()
        {
            EstDate = DateTime.Now,
            IsTaxApplicable = true,
            //GstLocSeller = "33"
        };
    }

/*    private decimal GetGSTWithinState()
    {
        if (EstimateWithTax)
        {
            if (Buyer?.GstStateCode == "33")
            {
                return Math.Round(SCGSTPercent / 2, 3);
            }

        };

        return 0M;
    }*/

    private decimal GetGSTPercent(string taxType = "SGST")
    {

        var gstPercent = _gstTaxRefList.FirstOrDefault
            (x => x.RefCode.Equals(taxType, StringComparison.OrdinalIgnoreCase));

        if (taxType.Equals("IGST", StringComparison.OrdinalIgnoreCase))
        {
            if (Buyer.Address.GstStateCode != Company.GstCode &&
                decimal.TryParse(gstPercent.RefValue.ToString(), out var igstPercent))
            {
                return igstPercent;
            }
            return 0M;
        }

        if (Buyer.Address.GstStateCode == Company.GstCode &&
            decimal.TryParse(gstPercent.RefValue.ToString(), out var result))
        {
            return result;
        }
        return 0M;
    }

    private void EvaluateForAllLines()
    {
        foreach (var line in Header.Lines)
        {
            EvaluateFormula(line);
        }
    }

    private void EvaluateFormula<T>(T item, bool isInit = false) where T : class
    {
        var formulas = FormulaStore.Instance.GetFormulas<T>();

        foreach (var formula in formulas)
        {
            //if (!isInit && IGNORE_UPDATE.Contains(formula.FieldName)) continue;

            var val = formula.Evaluate<T, decimal>(item, 0M);

            if (item is EstimateLine invLine)
                copyEstimateExpression[formula.FieldName].Invoke(invLine, val);
        }
    }

    private void EvaluateFormula<T>(T item, string fieldName, bool isInit = false) where T : class
    {
        //if (!isInit && IGNORE_UPDATE.Contains(fieldName)) return;

        var formula = FormulaStore.Instance.GetFormula<T>(fieldName);

        var val = formula.Evaluate<T, decimal>(item, 0M);

        if (item is EstimateLine invLine)
            copyEstimateExpression[fieldName].Invoke(invLine, val);
        else if (item is EstimateHeader head)
            copyHeaderExpression[fieldName].Invoke(head, val);
    }

    private void RefundVisible() => SetVisibilityForRefund();
    private void BalanceVisible() => SetVisibilityForRefund(isVisible: false);

    private void SetVisibilityForRefund(bool isVisible = true)
    {
        IsRefund = isVisible;
        IsBalance = !isVisible;
    }

    private bool CustomerCheck()
    {
        if (Buyer is null)
        {
            _messageBoxService.ShowMessage("Please enter customer details to proceed", "Missing Customer", MessageButton.OK, MessageIcon.Error);
            return false;
        }

        return true;
    }

}
