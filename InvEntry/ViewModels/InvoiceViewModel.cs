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
using System.Drawing.Text;
using System.Linq;
using System.Threading.Tasks;
using IDialogService = DevExpress.Mvvm.IDialogService;

namespace InvEntry.ViewModels;

public partial class InvoiceViewModel : ObservableObject
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
    private InvoiceHeader _header;

    [ObservableProperty]
    private InvoiceArReceipt _invoiceArReceipt;

/*    [ObservableProperty]
    private LedgersHeader _ledgerHeader;*/

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
    private ObservableCollection<InvoiceLine> selectedRows;

/*    [ObservableProperty]
    private ObservableCollection<ProductView> productStockList;*/

    [ObservableProperty]
    private ObservableCollection<string> productCategoryList;

    [ObservableProperty]
    private ObservableCollection<string> metalList;

    [ObservableProperty]
    private ObservableCollection<MtblReference> mtblReferencesList;

    [ObservableProperty]
    private ObservableCollection<MtblReference> salesPersonReferencesList;

    [ObservableProperty]
    private ObservableCollection<MtblReference> stateReferencesList;

    [ObservableProperty]
    private DateSearchOption _searchOption;

    private List<MtblReference> _gstTaxRefList;

    private bool createCustomer = false;
    private bool invBalanceChk = false;

    private readonly ICustomerService _customerService;
    private readonly IProductViewService _productViewService;
    private readonly IProductStockService _productStockService;
    private readonly IProductStockSummaryService _productStockSummaryService;
    private readonly IProductTransactionService _productTransactionService;
    //private readonly IProductTransactionSummaryService _productTransactionSummaryService;
    private readonly IDialogService _dialogService;
    private readonly IDialogService _reportDialogService;
    private readonly IMessageBoxService _messageBoxService;
    private readonly IVoucherService _voucherService;
    private readonly IInvoiceService _invoiceService;
    private readonly ILedgerService _ledgerService;
    private readonly IProductCategoryService _productCategoryService;
    private readonly IInvoiceArReceiptService _invoiceArReceiptService;
    private readonly IOrgThisCompanyViewService _orgThisCompanyViewService;
    private readonly IOldMetalTransactionService _oldMetalTransactionService;
    private readonly IMtblReferencesService _mtblReferencesService;
    private readonly IMtblLedgersService _mtblLedgersService;
    private readonly IReportFactoryService _reportFactoryService;
    private SettingsPageViewModel _settingsPageViewModel;
    private Dictionary<string, Action<InvoiceLine, decimal?>> copyInvoiceExpression;
    private Dictionary<string, Action<InvoiceHeader, decimal?>> copyHeaderExpression;

    private decimal IGSTPercent = 0M;
    private decimal SCGSTPercent = 3M;

    private List<string> IGNORE_UPDATE = new List<string>
    {
        nameof(InvoiceLine.VaAmount)
    };

    public InvoiceViewModel(ICustomerService customerService,
        IProductViewService productViewService,
        IProductStockService productStockService,
        IProductStockSummaryService productStockSummaryService,
        IProductTransactionService productTransactionService,
        //IProductTransactionSummaryService productTransactionSummaryService,
        IDialogService dialogService,
        IInvoiceService invoiceService,
        ILedgerService ledgerService,
        IProductCategoryService productCategoryService,
        IMessageBoxService messageBoxService,
        IVoucherService voucherService,
        IInvoiceArReceiptService invoiceArReceiptService,
        IOrgThisCompanyViewService orgThisCompanyViewService,
        IOldMetalTransactionService oldMetalTransactionService,
        IMtblReferencesService mtblReferencesService,
        IMtblLedgersService mtblLedgersService,
        SettingsPageViewModel settingsPageViewModel,
        IReportFactoryService reportFactoryService,
        [FromKeyedServices("ReportDialogService")] IDialogService reportDialogService)
    {

        _orgThisCompanyViewService = orgThisCompanyViewService;
        _customerService = customerService;
        _productViewService = productViewService;
        _productStockService = productStockService;
        _productStockSummaryService = productStockSummaryService;
        _productTransactionService = productTransactionService;
        //_productTransactionSummaryService = productTransactionSummaryService;
        _productCategoryService = productCategoryService;
        _dialogService = dialogService;
        _invoiceService = invoiceService;
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
        //productStockList = new();

        _customerReadOnly = false;

        _isBalance = true;
        _isRefund = false;
        _settingsPageViewModel = settingsPageViewModel;

        SetThisCompany();
        SetHeader();
        SetMasterLedger();

        PopulateProductCategoryList();
        PopulateStateList();
        PopulateUnboundLineDataMap();
        PopulateMtblRefNameList();
        PopulateMetalList();
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
        var stateRefList = new List<MtblReference>();

        var stateRefServiceList = await _mtblReferencesService.GetReferenceList("CUST_STATE");

        if (stateRefServiceList is null)
        {
            stateRefList.Add(new MtblReference() { RefValue = "Tamil Nadu", RefCode = "33" });
            stateRefList.Add(new MtblReference() { RefValue = "Kerala", RefCode = "32" });
            stateRefList.Add(new MtblReference() { RefValue = "Karnataka", RefCode = "30" });
        } else
        {
            stateRefList.AddRange(stateRefServiceList);
        }

        StateReferencesList = new(stateRefList);

        // CustomerState = StateReferencesList.FirstOrDefault(x => x.RefCode.Equals(Company.GstCode));
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

    private void PopulateUnboundLineDataMap()
    {
        if (copyInvoiceExpression is null) copyInvoiceExpression = new();

        copyInvoiceExpression.Add($"{nameof(InvoiceLine.InvlTaxableAmount)}", (item, val) => item.InvlTaxableAmount = val);
        copyInvoiceExpression.Add($"{nameof(InvoiceLine.ProdNetWeight)}", (item, val) => item.ProdNetWeight = val);
        copyInvoiceExpression.Add($"{nameof(InvoiceLine.InvlGrossAmt)}", (item, val) => item.InvlGrossAmt  = val * (item.Metal.Equals("DIAMOND") ? 100 : 1));
        copyInvoiceExpression.Add($"{nameof(InvoiceLine.VaAmount)}", (item, val) => item.VaAmount = val);
        copyInvoiceExpression.Add($"{nameof(InvoiceLine.InvlCgstAmount)}", (item, val) => item.InvlCgstAmount = val);
        copyInvoiceExpression.Add($"{nameof(InvoiceLine.InvlSgstAmount)}", (item, val) => item.InvlSgstAmount = val);
        copyInvoiceExpression.Add($"{nameof(InvoiceLine.InvlIgstAmount)}", (item, val) => item.InvlIgstAmount = val);
        copyInvoiceExpression.Add($"{nameof(InvoiceLine.InvlTotal)}", (item, val) => item.InvlTotal = val);
    }

    private void PopulateUnboundHeaderDataMap()
    {
        if (copyHeaderExpression is null) copyHeaderExpression = new();

        copyHeaderExpression.Add($"{nameof(InvoiceHeader.RoundOff)}", (item, val) => item.RoundOff = val);
        copyHeaderExpression.Add($"{nameof(InvoiceHeader.GrossRcbAmount)}", (item, val) => item.GrossRcbAmount = val);
        copyHeaderExpression.Add($"{nameof(InvoiceHeader.AmountPayable)}", (item, val) => item.AmountPayable = val);
        copyHeaderExpression.Add($"{nameof(InvoiceHeader.InvBalance)}", (item, val) => item.InvBalance = val);
    }

    partial void OnCustomerStateChanged(MtblReference value)
    {
        if (Buyer is null) return;

        Buyer.Address.GstStateCode = value.RefCode;

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
            Header.SalesPerson = value.RefValue;
        }
    }

    //might be introduced when SKU implmeneted
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

        }

        Header.CustMobile = phoneNumber;
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

        //this code will be re-introduce once SKU/Barcode is implmeneted
        //var product = ProductStockSelection();

        var productStk = await _productViewService.GetProduct(ProductIdUI);

        if (productStk is null)
        {
            _messageBoxService.ShowMessage($"No Product found for {ProductIdUI}, Please make sure it exists",
                "Product not found", MessageButton.OK, MessageIcon.Error);
            return;
        }


        //might introduce agains when barcode implemented
        //if (productStk is null)
        //{
        //    //No stock to be handled - let the user enter manually all the details of billing item
        //}

        var billedPrice = _settingsPageViewModel.GetPrice(productStk.Metal);

        InvoiceLine invoiceLine = new InvoiceLine()
        {
            ProdQty = 1,
            InvlBilledPrice = billedPrice,
            InvlCgstPercent = Header.CgstPercent,
            InvlSgstPercent = Header.SgstPercent,
            InvlIgstPercent = Header.IgstPercent,
            InvlStoneAmount = 0M,
            TaxType = "GST"
        };

        invoiceLine.SetProductDetails(productStk);

        EvaluateFormula(invoiceLine, isInit: true);

        Header.Lines.Add(invoiceLine);

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

    [RelayCommand]
    private Task EvaluateOldMetalTransaction()
    {
        //if (string.IsNullOrEmpty(OldMetalIdUI))
        //    return Task.CompletedTask;

        //var waitVM = WaitIndicatorVM.ShowIndicator("Fetching old product details...");

        // SplashScreenManager.CreateWaitIndicator(waitVM).Show();

        //var product = await _productService.GetProduct(OldMetalIdUI);
        // await Task.Delay(30000);

        // SplashScreenManager.ActiveSplashScreens.FirstOrDefault(x => x.ViewModel == waitVM).Close();

        //if (product is null)
        // {
        //     _messageBoxService.ShowMessage($"No Product found for {OldMetalIdUI}, Please make sure it exists",
        //         "Product not found", MessageButton.OK, MessageIcon.Error);
        //     return;
        //  }

        //   var billedPrice = _settingsPageViewModel.GetPrice(product.Metal);

        OldMetalTransaction oldMetalTransactionLine = new OldMetalTransaction()
        {
            CustGkey = Header.CustGkey,
            CustMobile = Header.CustMobile,
            TransType = "Purchase",
            TransDate = DateTime.Now,
            Uom = "Grams"
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

    [RelayCommand(CanExecute = nameof(CanCreateInvoice))]
    private async Task CreateInvoice()
    {
        LedgerHelper ledgerHelper = new(_ledgerService, _messageBoxService, _mtblLedgersService);   //is this a right way????? 

        invBalanceChk = true;  //is this a right place to fix
        var isSuccess = ProcessInvBalance();

        if (!isSuccess) return;

        if (!string.IsNullOrEmpty(Header.InvNbr))
        {
            var result = _messageBoxService.ShowMessage("Invoice already exists, Do you want to print preview the invoice ?", "Invoice", 
                                                            MessageButton.OKCancel, 
                                                            MessageIcon.Question, 
                                                            MessageResult.Cancel);

            if (result == MessageResult.OK)
            {
                PrintPreviewInvoice();
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
        Header.CustGkey = (int?)Buyer.GKey;

        // this loop required? - repetition???
        Header.Lines.ForEach(x =>
        {
            x.InvLineNbr = Header.Lines.IndexOf(x) + 1;
            x.InvoiceId = Header.InvNbr;
        });

        var header = await _invoiceService.CreateHeader(Header);

        if (header is not null)
        {
            Header.GKey = header.GKey;
            Header.InvNbr = header.InvNbr;

            Header.Lines.ForEach(x =>
            {
                x.InvoiceHdrGkey = header.GKey;
                x.InvoiceId = header.InvNbr;
            });

            // loop for validation check for customer
            await _invoiceService.CreateInvoiceLine(Header.Lines);

            await ProcessProductTransaction(Header.Lines);

            await ProcessOldMetalTransaction();

            //Invoice header details needs to be saved alongwith receipts, hence calling from here.
            ProcessReceipts();

            if ( (Header.AdvanceAdj > 0) || (Header.RdAmountAdj > 0) )
                await ledgerHelper.ProcessInvoiceAdvanceAsync(Header);

            _messageBoxService.ShowMessage("Invoice " + Header.InvNbr + " Created Successfully", "Invoice Created", 
                                                MessageButton.OK, MessageIcon.Exclamation);

            Messenger.Default.Send(MessageType.WaitIndicator, WaitIndicatorVM.ShowIndicator("Print Invoice..."));
            PrintPreviewInvoice();
            PrintPreviewInvoiceCommand.NotifyCanExecuteChanged();
            PrintInvoiceCommand.NotifyCanExecuteChanged();
            Messenger.Default.Send(MessageType.WaitIndicator, WaitIndicatorVM.HideIndicator());

        }
    }

    private async Task ProcessProductTransaction(IEnumerable<InvoiceLine> invLines)
    {
        foreach (var line in invLines)
        {

            await ProductStockSummaryUpdate(line);

            //await ProductStockUpdate(line);  //Put in on-hold for time being - till we introduce barcode

            //await CreateProductTransaction(line);
        }
    }

    private async Task ProductStockUpdate(InvoiceLine line)
    {

        var productStk = await _productStockService.GetProductStock(line.ProductSku);

        productStk.SoldWeight = line.ProdGrossWeight;
        productStk.BalanceWeight = 0;
        productStk.SoldQty = line.ProdQty;
        productStk.StockQty = 0;
        productStk.Status = "Sold";
        productStk.IsProductSold = true;
         
        await _productStockService.UpdateProductStock(productStk);

    }

    //Consolidated stock line in product stock summary - stock qty / weight has to be reduced based on invoiced qty
    //this logic would be revisited/changed once product sku - barcode feature introduced
    //WARN :- Ensure all product category must have one record in table
    private async Task ProductStockSummaryUpdate(InvoiceLine line)
    {
        ProductTransaction productTransaction = new();

        var productSumryStk = await _productStockSummaryService.GetByProductGkey(line.ProductGkey);

        //if (productSumryStk is null)
        //{
        //    await _productStockSummaryService.CreateProductStockSummary(productStockSummary);
        //}
        //else
        //{
        //    await _productStockSummaryService.UpdateProductStockSummary(productStockSummary);
        //}

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
            productTransaction.CbQty = productTransaction.ObQty.GetValueOrDefault() - line.ProdQty.GetValueOrDefault();

            productTransaction.TransactionGrossWeight = line.ProdGrossWeight.GetValueOrDefault();
            productTransaction.TransactionStoneWeight = line.ProdStoneWeight.GetValueOrDefault();
            productTransaction.TransactionNetWeight = line.ProdNetWeight.GetValueOrDefault();

            productTransaction.ClosingGrossWeight = productTransaction.OpeningGrossWeight.GetValueOrDefault()
                                                            - line.ProdGrossWeight.GetValueOrDefault();
            productTransaction.ClosingStoneWeight = productTransaction.OpeningStoneWeight.GetValueOrDefault()
                                                            - line.ProdStoneWeight.GetValueOrDefault();
            productTransaction.ClosingNetWeight = productTransaction.OpeningNetWeight.GetValueOrDefault()
                                                            - line.ProdNetWeight.GetValueOrDefault();

            //Set Product Stock Summary
            productSumryStk.GrossWeight = (productSumryStk.GrossWeight ?? 0) - line.ProdGrossWeight;
            productSumryStk.StoneWeight = (productSumryStk.StoneWeight ?? 0) - line.ProdStoneWeight;
            productSumryStk.NetWeight = (productSumryStk.NetWeight ?? 0) - line.ProdNetWeight;
            productSumryStk.SuppliedGrossWeight = (productSumryStk.SuppliedGrossWeight ?? 0) - line.ProdGrossWeight;
            //productSumryStk.AdjustedWeight = (productSumryStk.AdjustedWeight ?? 0);
            productSumryStk.SoldWeight = (productSumryStk.SoldWeight ?? 0) + line.ProdNetWeight;
            productSumryStk.BalanceWeight = (productSumryStk.BalanceWeight ?? 0) - line.ProdNetWeight;
            //productSumryStk.SuppliedQty = (productSumryStk.SuppliedQty ?? 0) + x.SuppliedQty;
            productSumryStk.SoldQty = (productSumryStk.SoldQty ?? 0) + line.ProdQty;
            productSumryStk.StockQty = (productSumryStk.StockQty ?? 0) - line.ProdQty;
            //productSumryStk.AdjustedQty = (productSumryStk.AdjustedQty ?? 0);

            await _productStockSummaryService.UpdateProductStockSummary(productSumryStk);

            productTransaction = await _productTransactionService.CreateProductTransaction(productTransaction);

            //await CreateProductTransaction(line, productSumryStk);
        }
    }

   /* private async void createProductTransactionSummary(ProductTransaction productTransaction)
    {

        ProductTransactionSummary productTransSumry = new();

        //Fetch last records of the day to set ob, cb etc
        SearchOption = new();
        SearchOption.To = DateTime.Today;
        SearchOption.From = DateTime.Today;
        SearchOption.Filter1 ??= productTransaction.ProductCategory;

        var prodTransSumry = await _productTransactionSummaryService.GetAll(SearchOption);

        productTransSumry = prodTransSumry.FirstOrDefault();

        if (productTransSumry != null)
        {
            // then add up with the existing total

            productTransSumry.StockOutQty = productTransSumry.StockOutQty.GetValueOrDefault() 
                                                + productTransaction.TransactionQty.GetValueOrDefault();
            productTransSumry.ClosingQty = productTransSumry.ClosingQty.GetValueOrDefault() 
                                                - productTransaction.TransactionQty.GetValueOrDefault();

            productTransSumry.StockOutGrossWeight = productTransSumry.StockOutGrossWeight.GetValueOrDefault() 
                                                                    + productTransaction.TransactionGrossWeight.GetValueOrDefault();
            productTransSumry.StockOutStoneWeight = productTransSumry.StockOutStoneWeight.GetValueOrDefault()
                                                                    + productTransaction.TransactionStoneWeight.GetValueOrDefault();
            productTransSumry.StockOutNetWeight = productTransSumry.StockOutNetWeight.GetValueOrDefault() 
                                                                    + productTransaction.TransactionNetWeight.GetValueOrDefault();

            productTransSumry.ClosingGrossWeight = productTransSumry.ClosingGrossWeight.GetValueOrDefault()
                                                                - productTransaction.TransactionGrossWeight.GetValueOrDefault();
            productTransSumry.ClosingStoneWeight = productTransSumry.ClosingStoneWeight.GetValueOrDefault()
                                                                - productTransaction.TransactionStoneWeight.GetValueOrDefault();
            productTransSumry.ClosingNetWeight = productTransSumry.ClosingNetWeight.GetValueOrDefault()
                                                                - productTransaction.TransactionNetWeight.GetValueOrDefault();

            await _productTransactionSummaryService.UpdateProductTransactionSummary(productTransSumry);
        }
        else
        {
            //create new record for the day if not found for todays 
            //get the last transaction of specific category to get opening balance
            ProductTransactionSummary prodTransSumryPrevious = new();

            productTransSumry = new();

            var prodTransSumryPrev = await _productTransactionSummaryService
                                                        .GetLastProductTranSumryByCategory(productTransaction.ProductCategory);
            if (prodTransSumryPrev != null) 
                prodTransSumryPrevious = prodTransSumryPrev;

            productTransSumry.TransactionDate = DateTime.Now;
            productTransSumry.ProductCategory = productTransaction.ProductCategory;
            productTransSumry.ProductSku = productTransaction.ProductSku;


            productTransSumry.StockInGrossWeight = 0;   //only stock entry
            productTransSumry.StockInStoneWeight = 0;
            productTransSumry.StockInNetWeight = 0;

            productTransSumry.StockOutGrossWeight = productTransaction.TransactionGrossWeight;
            productTransSumry.StockOutStoneWeight = productTransaction.TransactionStoneWeight;
            productTransSumry.StockOutNetWeight = productTransaction.TransactionNetWeight;

            //Opening
            productTransSumry.OpeningQty    = (prodTransSumryPrevious.ClosingQty ?? 0);
            productTransSumry.StockInQty    = 0;
            productTransSumry.StockOutQty   = productTransaction.TransactionQty.GetValueOrDefault();
            productTransSumry.ClosingQty    = productTransSumry.OpeningQty.GetValueOrDefault() 
                                                        - productTransaction.TransactionQty.GetValueOrDefault();

            productTransSumry.OpeningGrossWeight = (prodTransSumryPrevious.ClosingGrossWeight ?? 0);
            productTransSumry.OpeningStoneWeight = (prodTransSumryPrevious.ClosingStoneWeight ?? 0);
            productTransSumry.OpeningNetWeight = (prodTransSumryPrevious.ClosingNetWeight ?? 0);

            productTransSumry.ClosingGrossWeight = productTransSumry.OpeningGrossWeight.GetValueOrDefault()
                                                                    - productTransaction.TransactionGrossWeight;
            productTransSumry.ClosingStoneWeight = productTransSumry.OpeningStoneWeight.GetValueOrDefault()
                                                                    - productTransaction.TransactionStoneWeight;
            productTransSumry.ClosingNetWeight = productTransSumry.OpeningNetWeight.GetValueOrDefault()
                                                                    - productTransaction.TransactionNetWeight;

            await _productTransactionSummaryService.CreateProductTransactionSummary(productTransSumry);
        }

    }*/


    private bool CanCreateInvoice()
    {
        return string.IsNullOrEmpty(Header?.InvNbr);
    }

    [RelayCommand(CanExecute = nameof(CanPrintInvoice))]
    private void PrintInvoice()
    {
        var printed = PrintHelper.Print(_reportFactoryService.CreateInvoiceReport(Header.InvNbr));

        if (printed.HasValue && printed.Value)
            _messageBoxService.ShowMessage("Invoice printed Successfully", "Invoice print", 
                                                MessageButton.OK, MessageIcon.None);
    }

    private bool CanPrintInvoice()
    {
        return !CanCreateInvoice();
    }

    [RelayCommand(CanExecute = nameof(CanPrintInvoice))]
    private void PrintPreviewInvoice()
    {
        _reportDialogService.PrintPreview(Header.InvNbr);
        ResetInvoice();
    }

    [RelayCommand(CanExecute = nameof(CanPrintInvoice))]
    private void ExportToPdf()
    {
        _reportFactoryService.CreateInvoiceReportPdf(Header.InvNbr, "C:\\Madrone\\Invoice\\");
    }

    [RelayCommand]
    private void CellUpdate(CellValueChangedEventArgs args)
    {
        if (args.Row is InvoiceLine line)
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

    }

    [RelayCommand]
    private void EvaluateArRctLine(InvoiceArReceipt arInvRctLine)
    {

        if (string.IsNullOrEmpty(arInvRctLine.TransactionType))
        {
            return;
        }

        if (!arInvRctLine.BalBeforeAdj.HasValue)
            arInvRctLine.BalBeforeAdj = Header.InvBalance.GetValueOrDefault();

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

    private decimal FilterReceiptTransactions(string transType)
    {
        return (decimal)Header.ReceiptLines
                        .Where(x => transType.Equals(x.TransactionType, StringComparison.OrdinalIgnoreCase))
                        .Select(x => x.AdjustedAmount)
                        .Sum();
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

        Header.OldGoldAmount = FilterMetalTransactions("OLD GOLD");

        Header.OldSilverAmount = FilterMetalTransactions("OLD SILVER");

        // TaxableTotal from line without tax value
        Header.InvlTaxTotal = Header.Lines.Select(x => x.InvlTotal).Sum();

        // Line Taxable Total minus Old Gold & Silver Amount
        decimal BeforeTax = 0;
        BeforeTax = Header.InvlTaxTotal.GetValueOrDefault() -
                    Header.OldGoldAmount.GetValueOrDefault() -
                    Header.OldSilverAmount.GetValueOrDefault();

        if (BeforeTax >= 0)
        {
            Header.CgstAmount = MathUtils.Normalize(BeforeTax * Math.Round(Header.CgstPercent.GetValueOrDefault() / 100, 3));
            Header.SgstAmount = MathUtils.Normalize(BeforeTax * Math.Round(Header.SgstPercent.GetValueOrDefault() / 100, 3));
            Header.IgstAmount = MathUtils.Normalize(BeforeTax * Math.Round(Header.IgstPercent.GetValueOrDefault() / 100, 3));

        } else
        {
            Header.CgstAmount = 0;
            Header.SgstAmount = 0;
            Header.IgstAmount = 0;
        }

        Header.InvlTaxableAmount = BeforeTax;

        // After Tax Gross Value
        Header.GrossRcbAmount = BeforeTax +
                                Header.CgstAmount.GetValueOrDefault() +
                                Header.SgstAmount.GetValueOrDefault() +
                                Header.IgstAmount.GetValueOrDefault();

        decimal roundOff = 0;
        roundOff = Math.Round(Header.GrossRcbAmount.GetValueOrDefault(), 0) -
                        Header.GrossRcbAmount.GetValueOrDefault();

        Header.RoundOff = roundOff; 

        Header.GrossRcbAmount = MathUtils.Normalize(Header.GrossRcbAmount.GetValueOrDefault(), 0);

        decimal payableValue = 0;
        payableValue = Header.GrossRcbAmount.GetValueOrDefault() -
                        Header.DiscountAmount.GetValueOrDefault();

        Header.AmountPayable = MathUtils.Normalize(payableValue);

        Header.InvBalance = MathUtils.Normalize(Header.AmountPayable.GetValueOrDefault()) -
            (
                Header.RecdAmount.GetValueOrDefault() +
                Header.AdvanceAdj.GetValueOrDefault() +
                Header.RdAmountAdj.GetValueOrDefault()
             );


        if (invBalanceChk)
        {
            ProcessInvBalance();
        }
    }

    private bool ProcessInvBalance()
    {

        ProcessSettlements();

        //Note if inv balance is greater than zero - we need to show message to get confirmation from user
        // and warn to check there is unpaid balance........ 

        if (Header.InvBalance > 0)
        {
            var result = _messageBoxService.ShowMessage("Received Amount is less than Invoice Amount, " +
                "Do you want to make Credit for the balance Invoice Amount of Rs. " + Header.InvBalance + " ?", "Invoice", MessageButton.YesNo, MessageIcon.Question, MessageResult.No);

            if (result == MessageResult.Yes)
            {
                Header.PaymentDueDate = Header.InvDate.Value.AddDays(7);
                Header.InvRefund = 0M;
                BalanceVisible();
                SetReceipts("Credit");
            }
            else
            {
                return false;
            }
        }
        else if (Header.InvBalance == 0)
        {
            Header.PaymentDueDate = null;
            BalanceVisible();

        }
        else if (Header.InvBalance < 0)
        {
            var result = _messageBoxService.ShowMessage("Received Amount is more than Invoice Amount, " +
                "Do you want to Refund excess Amount of Rs. " + Header.InvBalance + " ?", "Invoice", MessageButton.YesNo, MessageIcon.Question, MessageResult.No);

            if (result == MessageResult.Yes)
            {
                Header.PaymentDueDate = null;
                Header.InvRefund = Header.InvBalance * -1;
                Header.InvBalance = 0M;
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

/*    private async void ProcessAdvance()
    {
        //check customer has already ledger entry
        LedgerHeader = await _ledgerService.GetHeader(MtblLedger.GKey, Buyer.GKey);   //hard coded to be fixed

        //{
        //    _messageBoxService.ShowMessage($"Available Advance Balance is  {ProductIdUI}, Please make sure it exists",
        //        "Product not found", MessageButton.OK, MessageIcon.Error);
        //    return;
        //}

        if (LedgerHeader is not null)
        {

            if ((LedgerHeader.CurrentBalance < 1) || (LedgerHeader.CurrentBalance < Header.AdvanceAdj.GetValueOrDefault()))
             {
                _messageBoxService.ShowMessage($"Available Advance Balance is Rs.  {LedgerHeader.CurrentBalance} only...",
                            "Insufficient Balance", MessageButton.OK, MessageIcon.Error);
                return;
            }

            LedgerHeader.CurrentBalance = LedgerHeader.CurrentBalance.GetValueOrDefault() - Header.AdvanceAdj.GetValueOrDefault();

            LedgersTransactions ledgerTrans = new();

            ledgerTrans.DrCr = "Cr";
            ledgerTrans.TransactionAmount = Header.AdvanceAdj;
            ledgerTrans.DocumentNbr = Header.InvNbr;
            ledgerTrans.DocumentDate = Header.InvDate;
            ledgerTrans.LedgerHdrGkey = LedgerHeader.GKey;
            ledgerTrans.TransactionDate = DateTime.Now;
            ledgerTrans.Status = true;

            LedgerHeader.Transactions.Add(ledgerTrans);

            await _ledgerService.CreateLedgersTransactions(LedgerHeader.Transactions);

            if (LedgerHeader.CurrentBalance < 0)
                LedgerHeader.CurrentBalance = 0;

            await _ledgerService.UpdateHeader(LedgerHeader);
        }
        else
        {

            LedgerHeader = new();

            LedgerHeader.MtblLedgersGkey = MtblLedger.GKey;
            LedgerHeader.CustGkey = Header.CustGkey;
            LedgerHeader.BalanceAsOn = DateTime.Now;

            LedgerHeader.CurrentBalance = 0; // Header.AdvanceAdj.GetValueOrDefault();

            if (LedgerHeader.CurrentBalance < 0)
                LedgerHeader.CurrentBalance = 0;

            LedgerHeader = await _ledgerService.CreateHeader(LedgerHeader);

            LedgersTransactions ledgerTrans = new();

            ledgerTrans.DrCr = "Cr";
            ledgerTrans.TransactionAmount = Header.AdvanceAdj.GetValueOrDefault();
            ledgerTrans.DocumentNbr = Header.InvNbr;
            ledgerTrans.DocumentDate = Header.InvDate;
            ledgerTrans.LedgerHdrGkey = LedgerHeader.GKey;
            ledgerTrans.TransactionDate = DateTime.Now;
            ledgerTrans.Status = true;

            LedgerHeader.Transactions.Add(ledgerTrans);

            await _ledgerService.CreateLedgersTransactions(LedgerHeader.Transactions);


        }

    }*/

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
        foreach(var receipts in Header.ReceiptLines)
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

        foreach(var omTrans in Header.OldMetalTransactions)
        {
            omTrans.EnrichHeaderDetails(Header);
        }

        await _oldMetalTransactionService.CreateOldMetalTransaction(Header.OldMetalTransactions);
    }

    private InvoiceArReceipt CreateArReceipts(InvoiceArReceipt invoiceArReceipt, Voucher voucher)
    {

        InvoiceArReceipt arInvRct = new()
        {
            //VoucherDate = DateTime.Now
        };

        arInvRct.SeqNbr                     = invoiceArReceipt.SeqNbr;
        arInvRct.CustGkey                   = invoiceArReceipt.CustGkey;
        arInvRct.InvoiceGkey                = (int?)Header.GKey;
        arInvRct.InvoiceNbr                 = Header.InvNbr;
        arInvRct.InvoiceReceivableAmount    = invoiceArReceipt.InvoiceReceivableAmount;
        arInvRct.BalanceAfterAdj            = invoiceArReceipt.BalanceAfterAdj;
        arInvRct.TransactionType            = invoiceArReceipt.TransactionType;
        arInvRct.ModeOfReceipt              = invoiceArReceipt.ModeOfReceipt;
        arInvRct.BalBeforeAdj               = invoiceArReceipt.BalBeforeAdj;
        arInvRct.InternalVoucherNbr         = voucher.VoucherNbr;
        arInvRct.InternalVoucherDate        = voucher.VoucherDate;
        arInvRct.InvoiceReceiptNbr          = Header.InvNbr.Replace("B", "R");  //hard coded - future review 
        arInvRct.Status                     = "Adj";

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
        Voucher.VoucherDate = Header.InvDate;
        Voucher.TransType = "Receipt";         // Trans_type    1 = Receipt,    2 = Payment,    3 = Journal
        Voucher.VoucherType = invoiceArReceipt.TransactionType; // Voucher_type  1 = Sales,      2 = Credit,     3 = Expense
        Voucher.Mode = invoiceArReceipt.ModeOfReceipt; // Mode          1 = Cash,       2 = Bank,       3 = Credit
        Voucher.TransDate = Voucher.VoucherDate;    // DateTime.Now;
        Voucher.VoucherNbr = Header.InvNbr;
        Voucher.RefDocNbr = Header.InvNbr;
        Voucher.RefDocDate = Header.InvDate;
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
           var s when s.Equals("Refund", StringComparison.OrdinalIgnoreCase) => Header.InvRefund,
           var s when s.Equals("Credit", StringComparison.OrdinalIgnoreCase) => Header.InvBalance,
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

            } catch (Exception e)
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
    private void ResetInvoice()
    {

        SetHeader();
        Buyer = null;
        CustomerPhoneNumber = null;
        CustomerState = null;
        SalesPerson = null;
        CreateInvoiceCommand.NotifyCanExecuteChanged();
        invBalanceChk = false;  //reset to false for next invoice
    }

    [RelayCommand(CanExecute = nameof(CanDeleteRows))]
    private void DeleteRows()
    {
        var result = _messageBoxService.ShowMessage("Delete all selected rows", "Delete Rows", MessageButton.YesNo, MessageIcon.Question, MessageResult.No);

        if (result == MessageResult.No)
            return;

        List<int> indexs = new List<int>();
       foreach(var row in SelectedRows)
       {
            indexs.Add(Header.Lines.IndexOf(row));
       }

        indexs.ForEach(x => 
        {
            if(x >= 0)
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
    private void DeleteSingleRow(InvoiceLine line)
    {
        var result = _messageBoxService.ShowMessage("Delete current row", "Delete Row", MessageButton.YesNo, MessageIcon.Question, MessageResult.No);

        if (result == MessageResult.No)
            return;

        var index = Header.Lines.Remove(line);
    }

    private bool CanDeleteSingleRow(InvoiceLine line)
    {
        return line is not null && Header.Lines.IndexOf(line) > -1;
    }

    private void SetHeader()
    {
        Header = new()
        {
            InvDate = DateTime.Now,
            IsTaxApplicable = true,
            GstLocSeller = Company.GstCode
        };
    }

    private async void SetThisCompany()
    {
        Company = new();
        Company = await _orgThisCompanyViewService.GetOrgThisCompany();
    }

    private decimal GetGSTPercent(string taxType = "SGST")
    {

        var gstPercent = _gstTaxRefList.FirstOrDefault
            (x => x.RefCode.Equals(taxType, StringComparison.OrdinalIgnoreCase));

        if (taxType.Equals("IGST",StringComparison.OrdinalIgnoreCase))
        {
            if (Buyer.Address.GstStateCode != Company.GstCode &&
                decimal.TryParse(gstPercent.RefValue.ToString(), out var igstPercent)   )
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

            if (item is InvoiceLine invLine)
                copyInvoiceExpression[formula.FieldName].Invoke(invLine, val);
        }
    }

    private void EvaluateFormula<T>(T item, string fieldName, bool isInit = false) where T : class
    {
        //if (!isInit && IGNORE_UPDATE.Contains(fieldName)) return;

        var formula = FormulaStore.Instance.GetFormula<T>(fieldName);

        var val = formula.Evaluate<T, decimal>(item, 0M);

        if (item is InvoiceLine invLine)
            copyInvoiceExpression[fieldName].Invoke(invLine, val);
        else if (item is InvoiceHeader head)
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
        if(Buyer is null)
        {
            _messageBoxService.ShowMessage("Please enter customer details to proceed", "Missing Customer", MessageButton.OK, MessageIcon.Error);
            return false;
        }

        return true;
    }
}