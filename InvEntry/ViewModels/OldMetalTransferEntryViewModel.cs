using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DevExpress.CodeParser;
using DevExpress.Mvvm;
using DevExpress.Mvvm.Native;
using DevExpress.Xpf.Core;
using DevExpress.Xpf.Editors;
using DevExpress.Xpf.Printing;
using DevExpress.Xpf.WindowsUI.Navigation;
using InvEntry.Extension;
using InvEntry.Helper;
using InvEntry.Models;
using InvEntry.Models.Extensions;
using InvEntry.Reports;
using InvEntry.Services;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
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
    private ObservableCollection<MtblReference> _receipientsList;

    private readonly IEstimateService _estimateService;
    private readonly IOrgThisCompanyViewService _orgThisCompanyViewService;
    private readonly IMtblReferencesService _mtblReferencesService;
    private readonly ICustomerService _customerService;
    private readonly IProductViewService _productViewService;

    private SettingsPageViewModel _settingsPageViewModel;

    private readonly IDialogService _dialogService;
    private readonly IDialogService _reportDialogService;
    private readonly IMessageBoxService _messageBoxService;
    private readonly IReportFactoryService _reportFactoryService;

    private decimal todaysRate;

    public OldMetalTransferEntryViewModel(
                    IDialogService dialogService,
                    IMessageBoxService messageBoxService,
                    IEstimateService estimateService,
                    IOrgThisCompanyViewService orgThisCompanyViewService,
                    IMtblReferencesService mtblReferencesService,
                    ICustomerService customerService,
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

        ReceipientStrList = new(branchToRefList.Select(x => x.RefCode)); //[.. branchToRefList.Select(x => x.RefCode)];

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


    partial void OnSentToChanged(string value)
    {
        //if (Buyer is null) return;
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

    [RelayCommand]
    private async Task FetchProduct()
    {
        if (string.IsNullOrEmpty(OldMetalIdUI)) return;

        var waitVM = WaitIndicatorVM.ShowIndicator("Fetching product details...");

        SplashScreenManager.CreateWaitIndicator(waitVM).Show();

        var product = await _productViewService.GetProduct(OldMetalIdUI);

        // await Task.Delay(30000);

        SplashScreenManager.ActiveSplashScreens.FirstOrDefault(x => x.ViewModel == waitVM).Close();

        if (product is null)
        {
            _messageBoxService.ShowMessage($"No Product found for {OldMetalIdUI}, Please make sure it exists",
                "Product not found", MessageButton.OK, MessageIcon.Error);
            return;
        }

        var metalPrice = _settingsPageViewModel.GetPrice(product.Metal);

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

        estimateLine.SetProductDetails(product);

        //EvaluateFormula(estimateLine, isInit: true);

        Header.Lines.Add(estimateLine);

        OldMetalIdUI = string.Empty;

        //EvaluateHeader();

    }

    [RelayCommand(CanExecute = nameof(CanCreateStockTransfer))]
    private async Task CreateStockTransfer()
    {
        //LedgerHelper ledgerHelper = new(_ledgerService, _messageBoxService, _mtblLedgersService);   //is this a right way????? 

        //estBalanceChk = true;  //is this a right place to fix
        //var isSuccess = ProcessEstBalance();

        //if (!isSuccess) return;

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

/*        if (createCustomer)
        {
            Buyer = await _customerService.CreateCustomer(Buyer);
        }*/

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
           // to do await _estimateService.CreateEstimateLine(Header.Lines);

        // Review >>>>>>   if (IsStockTransfer)
        //        await ProcessProductTransaction(Header.Lines);

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
        //to do ResetEstimate();
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
    }

    private bool CanPrintStockTransfer()
    {
        return !CanCreateStockTransfer();
    }

}


