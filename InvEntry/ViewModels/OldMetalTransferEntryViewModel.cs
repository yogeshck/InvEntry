using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DevExpress.Mvvm;
using DevExpress.Mvvm.Native;
using DevExpress.Xpf.Core;
using DevExpress.Xpf.Editors;
using DevExpress.Xpf.Grid;
using DevExpress.Xpf.Layout.Core;
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

public partial class OldMetalTransferEntryViewModel: ObservableObject
{

    [ObservableProperty]
    private Customer _buyer;

    [ObservableProperty]
    private OrgThisCompanyView _company;

    [ObservableProperty]
    private EstimateHeader _header;

    [ObservableProperty]
    private MtblReference _customerState;

    [ObservableProperty]
    private ObservableCollection<string> _oldMetalList;

    [ObservableProperty]
    private ObservableCollection<string> _receipientStrList;

    [ObservableProperty]
    private string _fromBranch;

    [ObservableProperty]
    private string _oldMetalIdUI;

    [ObservableProperty]
    private string _sentTo;

    [ObservableProperty]
    private decimal _productGrossWeight;

    [ObservableProperty]
    private string _oMTransDesc;

    [ObservableProperty]
    private ObservableCollection<MtblReference> _receipientsList;

    private readonly IEstimateService _estimateService;
    private readonly IOrgThisCompanyViewService _orgThisCompanyViewService;
    private readonly IMtblReferencesService _mtblReferencesService;
    private readonly ICustomerService _customerService;
    private readonly IProductViewService _productViewService;
    private readonly IOldMetalTransactionService _oldMetalTransactionService;

    private SettingsPageViewModel _settingsPageViewModel;
    private Dictionary<string, Action<EstimateLine, decimal?>> copyEstimateExpression;
    private Dictionary<string, Action<EstimateHeader, decimal?>> copyHeaderExpression;

    private readonly IDialogService _dialogService;
    private readonly IDialogService _reportDialogService;
    private readonly IMessageBoxService _messageBoxService;
    private readonly IReportFactoryService _reportFactoryService;

    private decimal todaysRate;
    private ProductView OldMetalProduct;

    public OldMetalTransferEntryViewModel(
                    IDialogService dialogService,
                    IMessageBoxService messageBoxService,
                    IEstimateService estimateService,
                    IOrgThisCompanyViewService orgThisCompanyViewService,
                    IMtblReferencesService mtblReferencesService,
                    ICustomerService customerService,
                    IOldMetalTransactionService oldMetalTransactionService,
                    SettingsPageViewModel settingsPageViewModel,
                    IProductViewService productViewService,
                    IReportFactoryService reportFactoryService,
                    [FromKeyedServices("ReportDialogService")] IDialogService reportDialogService
    ) 
    {
        _estimateService = estimateService;
        _orgThisCompanyViewService = orgThisCompanyViewService;
        _mtblReferencesService = mtblReferencesService;
        _customerService = customerService;
        _productViewService = productViewService;
        _settingsPageViewModel = settingsPageViewModel;
        _oldMetalTransactionService = oldMetalTransactionService;

        _messageBoxService = messageBoxService;
        _reportDialogService = reportDialogService;
        _reportFactoryService = reportFactoryService;


        SetThisCompany();
        SetHeader();

        PopulateReceipientList();
        PopulateOldMetalList();

    }

    private void SetHeader()
    {
        Header = new()
        {
            EstDate = DateTime.Now,
            IsTaxApplicable = false,
        };
    }

    private decimal getBilledPrice(string metal)
    {
        var metalPrice = _settingsPageViewModel.GetPrice(metal);

        if (metalPrice < 1)
        {
            displayRateErrorMsg();
            //metalPrice = -1;
        }

        todaysRate = (decimal)metalPrice;

        return (decimal)metalPrice;
    }

    private async void SetThisCompany()
    {

        Company = new();
        Company = await _orgThisCompanyViewService.GetOrgThisCompany();

        Header.TenantGkey = Company.TenantGkey;
        Header.GstLocSeller = Company.GstCode;

        FromBranch = Company.CompanyName;

    }

    private async void PopulateReceipientList()
    {
        var branchToRefList = await _mtblReferencesService.GetReferenceList("STOCK_TRANSFER");

        ReceipientStrList = new(branchToRefList.Select(x => x.RefCode));  

        ReceipientsList = new(branchToRefList);

    }

    private async void PopulateOldMetalList()
    {
        var metalRefList = await _mtblReferencesService.GetReferenceList("OLD_METALS");
        OldMetalList = new(metalRefList.Select(x => x.RefValue));
    }

    private void displayRateErrorMsg()
    {
        _messageBoxService.ShowMessage($"Todays Rate not updated in system, set the rate and start estimate....",
                                        "Todays Rate not found", MessageButton.OK, MessageIcon.Error);
    }

    [RelayCommand]
    private void ResetOldMetalTrans()
    {
        // var result = _messageBoxService.ShowMessage("Reset all values", "Reset Invoice", MessageButton.YesNo, MessageIcon.Question, MessageResult.No);

        // if (result == MessageResult.No)
        //     return;

        SetHeader();
        SetThisCompany();

        Buyer = null;
        CustomerState = null;
        SentTo = null;
        OldMetalIdUI = string.Empty;
        ProductGrossWeight = 0;
        OMTransDesc = string.Empty;

        CreateStockTransferCommand.NotifyCanExecuteChanged();

    }

    partial void OnSentToChanged(string value)
    {
       
        var receipient = string.Empty;

        if (value is not null)
        {
            receipient = ReceipientsList.Where(x => x.RefCode == value).Select(x => x.RefValue).FirstOrDefault();

        } else 
        { 
            return; 
        }

        Header.CustMobile = receipient;

        _ = SetBuyer();
    }

    public async Task SetBuyer() 
    {

        //var branchTo = await _mtblReferencesService.GetReference("STOCK_TRANSFER", Header.CustMobile);
        var contactNbr = Header.CustMobile;

        Messenger.Default.Send(MessageType.WaitIndicator, WaitIndicatorVM.ShowIndicator("Fetching Customer details..."));

        Buyer = await _customerService.GetCustomer(contactNbr);

        Messenger.Default.Send(MessageType.WaitIndicator, WaitIndicatorVM.HideIndicator());

        if (Buyer is null)
        {
            _messageBoxService.ShowMessage("No customer details found.", "Customer not found", MessageButton.OK);

            Buyer = new();
            Buyer.MobileNbr = contactNbr.Trim();
            Buyer.Address.GstStateCode = Company.GstCode;
            Buyer.Address.State = Company.State;
            Buyer.Address.District = Company.District;

        //    createCustomer = true;
        //Error    CustomerState = StateReferencesList.FirstOrDefault(x => x.RefCode == Company.GstCode);
            
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

            //Error >>>>>CustomerState = StateReferencesList.FirstOrDefault(x => x.RefCode == gstCode);

            Messenger.Default.Send("ProductIdUIName", MessageType.FocusTextEdit);
        }

        Header.CustMobile = contactNbr;

        //to effect stock update though it is just estimate - being used for stock transfer to other branches
        //  foreach (var item in StkTrfrList)
        //  {
        //      if (item is not null && item == phoneNumber)
        //          IsStockTransfer = true;
        //  }

    }

    private async Task FetchProduct()
    {
        if (string.IsNullOrEmpty(OldMetalIdUI)) return;

        var waitVM = WaitIndicatorVM.ShowIndicator("Fetching product details...");

        SplashScreenManager.CreateWaitIndicator(waitVM).Show();

        OldMetalProduct = await _productViewService.GetProduct(OldMetalIdUI);

        // await Task.Delay(30000);

        SplashScreenManager.ActiveSplashScreens.FirstOrDefault(x => x.ViewModel == waitVM).Close();

        if (OldMetalProduct is null)
        {
            _messageBoxService.ShowMessage($"No Product found for {OldMetalIdUI}, Please make sure it exists",
                "Product not found", MessageButton.OK, MessageIcon.Error);
            return;
        }

        var metalPrice = getBilledPrice(OldMetalProduct.Metal);    // _settingsPageViewModel.GetPrice(product.Metal);

        if (metalPrice < 1)
        {
            displayRateErrorMsg();
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

        estimateLine.SetProductDetails(OldMetalProduct);

        //EvaluateFormula(estimateLine, isInit: true);

        SetLineDetails(estimateLine);

        Header.Lines.Add(estimateLine);

        //OldMetalIdUI = string.Empty;

        EvaluateHeader();

    }

    private void SetLineDetails(EstimateLine estLine)
    {

        estLine.EstLineNbr = 1;
        estLine.ProdGrossWeight = ProductGrossWeight;
        estLine.ProdStoneWeight = 0;
        estLine.ProdNetWeight = ProductGrossWeight;

    }

    private void SetOldMetalTransaction()
    {

        OldMetalTransaction oldMetalTransaction = new()
        {
            TransDate = DateTime.Now
        };

        oldMetalTransaction.EnrichEstHeaderOMTransDetails(Header);
        oldMetalTransaction.EnrichProductDetails(OldMetalProduct);

        if (oldMetalTransaction.TransactedRate.GetValueOrDefault() < 1)
            oldMetalTransaction.TransactedRate = todaysRate;

        oldMetalTransaction.Uom = "Grams";
        oldMetalTransaction.GrossWeight = ProductGrossWeight;
        oldMetalTransaction.StoneWeight = 0;

        oldMetalTransaction.NetWeight = (
                                           oldMetalTransaction.GrossWeight.GetValueOrDefault() -
                                           oldMetalTransaction.StoneWeight.GetValueOrDefault() -
                                           oldMetalTransaction.WastageWeight.GetValueOrDefault()
                                        );

        oldMetalTransaction.TotalProposedPrice = oldMetalTransaction.NetWeight.GetValueOrDefault() *
                                                    oldMetalTransaction.TransactedRate.GetValueOrDefault();
        oldMetalTransaction.FinalPurchasePrice = oldMetalTransaction.TotalProposedPrice;

        oldMetalTransaction.Remarks = Header.EstNotes;

        Header.OldMetalTransactions.Add(oldMetalTransaction);

    }

    private void EvaluateHeader()
    {

        // Header.AdvanceAdj = FilterReceiptTransactions("Advance");
        // Header.RdAmountAdj = FilterReceiptTransactions("RD");

        Header.RecdAmount = Header.ReceiptLines.Select(x => x.AdjustedAmount).Sum();

        Header.OldGoldAmount = 0;
        /*FilterMetalTransactions("OLD GOLD 18KT") 
                                + FilterMetalTransactions("OLD GOLD 22KT") 
                                + FilterMetalTransactions("OLD GOLD 916-22KT"); */

        Header.OldSilverAmount = 0; // FilterMetalTransactions("OLD SILVER");

        // TaxableTotal from line without tax value
        Header.EstlTaxTotal = Header.Lines.Select(x => x.EstlTotal).Sum();

        // Line Taxable Total minus Old Gold & Silver Amount
        decimal BeforeTax = 0;
        BeforeTax = Header.EstlTaxTotal.GetValueOrDefault() -
                    Header.OldGoldAmount.GetValueOrDefault() -
                    Header.OldSilverAmount.GetValueOrDefault();


        if (BeforeTax >= 0) // && EstimateWithTax)
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

/*        if (estBalanceChk)
        {
            ProcessEstBalance();
        }*/
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

    [RelayCommand(CanExecute = nameof(CanCreateStockTransfer))]
    private async Task CreateStockTransfer()
    {

        await FetchProduct();

        if (!string.IsNullOrEmpty(Header.EstNbr))
        {
            var result = _messageBoxService.ShowMessage("Transfer already created, Do you want to print preview the Transfer ?", "Estoice", MessageButton.OKCancel, MessageIcon.Question, MessageResult.Cancel);

            if (result == MessageResult.OK)
            {
                PrintPreviewStockTransfer();
            }
            return;
        }

        if (Buyer is null || string.IsNullOrEmpty(Buyer.CustomerName))
        {
            _messageBoxService.ShowMessage("Customer information is not provided", "Customer info", MessageButton.OK, MessageIcon.Hand);
            return;
        }

/*      if (createCustomer)
        {
            Buyer = await _customerService.CreateCustomer(Buyer);
        }*/

        Header.CustGkey = (int?)Buyer.GKey;
        Header.EstNotes = OMTransDesc;

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

            SetOldMetalTransaction();
            await _oldMetalTransactionService.CreateOldMetalTransaction(Header.OldMetalTransactions);

            _messageBoxService.ShowMessage("Transfer Created Successfully", "Transfer Created", MessageButton.OK, MessageIcon.Exclamation);

            Messenger.Default.Send(MessageType.WaitIndicator, WaitIndicatorVM.ShowIndicator("Print Transfer..."));

            var waitVM = WaitIndicatorVM.ShowIndicator("Please wait.... preparing print document.... .");
            SplashScreenManager.CreateWaitIndicator(waitVM).Show();

            PrintPreviewStockTransfer();

            SplashScreenManager.ActiveSplashScreens.FirstOrDefault(x => x.ViewModel == waitVM).Close();

            PrintPreviewStockTransferCommand.NotifyCanExecuteChanged();
            PrintStockTransferCommand.NotifyCanExecuteChanged();
            Messenger.Default.Send(MessageType.WaitIndicator, WaitIndicatorVM.HideIndicator());

        }
    }

    private bool CanCreateStockTransfer()
    {
        return string.IsNullOrEmpty(Header?.EstNbr);
    }

    [RelayCommand(CanExecute = nameof(CanPrintStockTransfer))]
    private void PrintPreviewStockTransfer()
    {
        _reportDialogService.PrintPreviewEstimate(Header.EstNbr, Header.GKey, Company);
        ResetOldMetalTrans();
    }

    [RelayCommand(CanExecute = nameof(CanPrintStockTransfer))]
    private void ExportToPdf()
    {
        _reportFactoryService.CreateEstimateReportPdf(Header.EstNbr, Header.GKey, Company, "D:\\Madrone\\Invoice\\");
    }

    [RelayCommand(CanExecute = nameof(CanPrintStockTransfer))]
    private void PrintStockTransfer()
    {
        //after report uncomment this
        var printed = PrintHelper.Print(_reportFactoryService.CreateEstimateReport(Header.EstNbr, Header.GKey, Company));

        if (printed.HasValue && printed.Value)
            _messageBoxService.ShowMessage("Estimate printed Successfully", "Estimate print", MessageButton.OK, MessageIcon.None);

        ResetOldMetalTrans();
    }

    private bool CanPrintStockTransfer()
    {
        return !CanCreateStockTransfer();
    }

}


