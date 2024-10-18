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
using System.Windows.Input;
using IDialogService = DevExpress.Mvvm.IDialogService;

namespace InvEntry.ViewModels;

public partial class InvoiceViewModel : ObservableObject
{
    [ObservableProperty]
    private string _customerPhoneNumber;

    [ObservableProperty]
    private Customer _buyer;

    [ObservableProperty]
    private InvoiceHeader _header;

    [ObservableProperty]
    private InvoiceArReceipt _invoiceArReceipt;

    [ObservableProperty]
    private string _productIdUI;

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

    [ObservableProperty]
    private ObservableCollection<string> productCategoryList;

    [ObservableProperty]
    private ObservableCollection<MtblReference> mtblReferencesList;
  
    public ICommand ShowWindowCommand { get; set; }

    private bool createCustomer = false;
    private bool invBalanceChk = false;

    private readonly ICustomerService _customerService;
    private readonly IProductService _productService;
    private readonly IDialogService _dialogService;
    private readonly IDialogService _reportDialogService;
    private readonly IMessageBoxService _messageBoxService;
    private readonly IVoucherService _voucherService;
    private readonly IInvoiceService _invoiceService;
    private readonly IProductCategoryService _productCategoryService;
    private readonly IInvoiceArReceiptService _invoiceArReceiptService;
    private readonly IOldMetalTransactionService _oldMetalTransactionService;
    private readonly IMtblReferencesService _mtblReferencesService;
    private readonly IReportFactoryService _reportFactoryService;
    private SettingsPageViewModel _settingsPageViewModel;
    private Dictionary<string, Action<InvoiceLine, decimal?>> copyInvoiceExpression;
    private Dictionary<string, Action<InvoiceHeader, decimal?>> copyHeaderExpression;
    private decimal IGSTPercent = 0M;
    private decimal SCGSTPercent = 3M;
    private decimal BalToAdjust = 0;

    private List<string> IGNORE_UPDATE = new List<string>
    {
        nameof(InvoiceLine.VaAmount)
    };

    public InvoiceViewModel(ICustomerService customerService,
        IProductService productService,
        IDialogService dialogService,
        IInvoiceService invoiceService,
        IProductCategoryService productCategoryService,
        IMessageBoxService messageBoxService,
        IVoucherService voucherService,
        IInvoiceArReceiptService invoiceArReceiptService,
        IMtblReferencesService mtblReferencesService,
        SettingsPageViewModel settingsPageViewModel,
        IReportFactoryService reportFactoryService,
        [FromKeyedServices("ReportDialogService")]IDialogService reportDialogService)
    {

        //MtblRefNameList = new();

        SetHeader();
        _customerService = customerService;
        _productService = productService;
        _productCategoryService = productCategoryService;
        _dialogService = dialogService;
        _invoiceService = invoiceService;
        _messageBoxService = messageBoxService;
        _reportDialogService = reportDialogService;
        _reportFactoryService = reportFactoryService;
        _voucherService = voucherService;
        _invoiceArReceiptService = invoiceArReceiptService;
        _mtblReferencesService = mtblReferencesService;

        selectedRows = new();
        _customerReadOnly = false;

        _isBalance = true;
        _isRefund = false;
        _settingsPageViewModel = settingsPageViewModel;

        PopulateProductCategoryList();
        PopulateUnboundLineDataMap();
        PopulateMtblRefNameList();
        //PopulateUnboundHeaderDataMap();
    }

    private async void PopulateProductCategoryList()
    {
        var list = await _productCategoryService.GetProductCategoryList();
        ProductCategoryList = new(list.Select(x => x.Name));
    }

    private async void PopulateMtblRefNameList()
    {
        var mtblRefList = await _mtblReferencesService.GetReferenceList("PAYMENT_MODE");
        MtblReferencesList = new(mtblRefList);

        //to modify and fetch from db using service
/*                MtblRefNameList.Add("Advance");
                MtblRefNameList.Add("Card");
                MtblRefNameList.Add("Cash");
                MtblRefNameList.Add("GPAY");
                MtblRefNameList.Add("RD");*/
    }

    private void PopulateUnboundLineDataMap()
    {
        if (copyInvoiceExpression is null) copyInvoiceExpression = new();

        copyInvoiceExpression.Add($"{nameof(InvoiceLine.InvlTaxableAmount)}", (item, val) => item.InvlTaxableAmount = val);
        copyInvoiceExpression.Add($"{nameof(InvoiceLine.ProdNetWeight)}", (item, val) => item.ProdNetWeight = val);
        copyInvoiceExpression.Add($"{nameof(InvoiceLine.InvlGrossAmt)}", (item, val) => item.InvlGrossAmt = val);
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

    [RelayCommand]
    private void AddReceipts()
    {
        var vm = new InvoiceReceiptsViewModel();
        var result = _dialogService.ShowDialog(MessageButton.OKCancel, "Receipts", "InvoiceReceiptsView", vm);
    
        if (result == MessageResult.OK)
        {

        } else
        {

        }
    }

    [RelayCommand]
    private async Task FetchCustomer(EditValueChangedEventArgs args)
    {
        if (args.NewValue is not string phoneNumber) return;

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
            createCustomer = true;

            Buyer.GstStateCode = "33";    //Need to fetch based on pincode - future change
            Header.GstLocBuyer = Buyer.GstStateCode;
            Header.CgstPercent = GetGSTWithinState();
            Header.SgstPercent = GetGSTWithinState();
            Header.IgstPercent = IGSTPercent;

            Messenger.Default.Send("CustomerNameUI", MessageType.FocusTextEdit);
        }
        else
        {
            Header.GstLocBuyer = Buyer.GstStateCode;
            Messenger.Default.Send("ProductIdUIName", MessageType.FocusTextEdit);
            IGSTPercent = Buyer.GstStateCode == "33" ? 0M : 3M;

            Header.CgstPercent = GetGSTWithinState();
            Header.SgstPercent = GetGSTWithinState();
            Header.IgstPercent = IGSTPercent;
        }

        Header.CustMobile = phoneNumber;
    }

    [RelayCommand]
    private async Task FetchProduct()
    {
        if (string.IsNullOrEmpty(ProductIdUI)) return;

        var waitVM = WaitIndicatorVM.ShowIndicator("Fetching product details...");
  
        SplashScreenManager.CreateWaitIndicator(waitVM).Show();

        var product = await _productService.GetProduct(ProductIdUI);
       // await Task.Delay(30000);

        SplashScreenManager.ActiveSplashScreens.FirstOrDefault(x => x.ViewModel == waitVM).Close();

        if (product is null)
        {
            _messageBoxService.ShowMessage($"No Product found for {ProductIdUI}, Please make sure it exists",
                "Product not found", MessageButton.OK, MessageIcon.Error);
            return;
        }

        var billedPrice = _settingsPageViewModel.GetPrice(product.Metal);

        InvoiceLine invoiceLine = new InvoiceLine()
        {
            ProdQty = 1,
            InvlBilledPrice = billedPrice,
            InvlCgstPercent = GetGSTWithinState(),
            InvlSgstPercent = GetGSTWithinState(),
            InvlIgstPercent = IGSTPercent,
            InvlStoneAmount = 0M,
            TaxType = "GST"
        };


            invoiceLine.SetProductDetails(product);

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
    private async Task EvaluateOldMetalTransaction()
    {
         if (string.IsNullOrEmpty(OldMetalIdUI)) return;

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
            CustMobile = Header.CustMobile
        };


        //oldMetalTransactionLine.SetProductDetails(product);

        //EvaluateFormula(invoiceLine, isInit: true);

        Header.OldMetalTransactions.Add(oldMetalTransactionLine);

        OldMetalIdUI = string.Empty;

       // EvaluateHeader();

    }

    [RelayCommand]
    private async Task ProcessArReceipts()    
    {
        //  var paymentMode = await _mtblReferencesService.GetReference("PAYMENT_MODE");
        // _productService.GetProduct(ProductIdUI);

        // var waitVM = WaitIndicatorVM.ShowIndicator("Fetching Invoice Receipt details...");

        var noOfLines = Header.ReceiptLines.Count;

        InvoiceArReceipt arInvRctLine = new InvoiceArReceipt()
        {
            //BalBeforeAdj = Header.GrossRcbAmount, 
            CustGkey = Header.CustGkey,      
            Status = "Open",    //Status Open - Before Adjustment
            SeqNbr = noOfLines + 1
        };

        Header.ReceiptLines.Add(arInvRctLine);
  
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

        //OMP - Old Metal Purchase
/*        OMP.Metal = OmpUI.Metal;
        OMP.Purity = OmpUI.Purity;*/
    }

    [RelayCommand(CanExecute = nameof(CanCreateInvoice))]
    private async Task CreateInvoice()
    {
        invBalanceChk = true;  //is this a right place to fix
        ProcessInvBalance();

        if (!string.IsNullOrEmpty(Header.InvNbr))
        {
            var result = _messageBoxService.ShowMessage("Invoice already created, Do you want to print preview the invoice ?", "Invoice", MessageButton.OKCancel, MessageIcon.Question, MessageResult.Cancel);

            if(result == MessageResult.OK)
            {
                PrintPreviewInvoice();
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
            Buyer = await _customerService.CreatCustomer(Buyer);
        }

        //Header.InvNbr = InvoiceNumberGenerator.Generate();
        Header.CustGkey = (int?)Buyer.GKey;

        Header.Lines.ForEach(x =>
        {
            x.InvLineNbr = Header.Lines.IndexOf(x) + 1;
            x.InvoiceId = Header.InvNbr;
        });

        var header = await _invoiceService.CreatHeader(Header);

        if(header is not null)
        {
            Header.GKey = header.GKey;
            Header.InvNbr = header.InvNbr;
            Header.Lines.ForEach(x => 
            {
                x.InvoiceHdrGkey = header.GKey; 
                x.InvoiceId = header.InvNbr;
            });
            // loop for validation check for customer
            await _invoiceService.CreatInvoiceLine(Header.Lines);
            _messageBoxService.ShowMessage("Invoice Created Successfully", "Invoice Created", MessageButton.OK, MessageIcon.Exclamation);

            ProcessOldMetalTransaction();
            //Invoice header details needs to be saved alongwith receipts, hence calling from here.
            ProcessReceipts();

            Messenger.Default.Send(MessageType.WaitIndicator, WaitIndicatorVM.ShowIndicator("Print Invoice..."));
            PrintPreviewInvoice();
            PrintPreviewInvoiceCommand.NotifyCanExecuteChanged();
            PrintInvoiceCommand.NotifyCanExecuteChanged();
            Messenger.Default.Send(MessageType.WaitIndicator, WaitIndicatorVM.HideIndicator());
 
        }
    }

    private bool CanCreateInvoice()
    {
        return string.IsNullOrEmpty(Header?.InvNbr);
    }

    [RelayCommand(CanExecute = nameof(CanPrintInvoice))]
    private void PrintInvoice()
    {
        var printed = PrintHelper.Print(_reportFactoryService.CreateInvoiceReport(Header.InvNbr));

        if(printed.HasValue && printed.Value)
            _messageBoxService.ShowMessage("Invoice printed Successfully", "Invoice print", MessageButton.OK, MessageIcon.None);
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
        if (args.Row is OldMetalTransaction oldMetalTransaction)
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


       //     arInvRctLine.BalBeforeAdj = BalToAdjust;
       // BalToAdjust = BalToAdjust - arInvRctLine.AdjustedAmount.GetValueOrDefault();
       // arInvRctLine.BalanceAfterAdj = BalToAdjust;

       // Header.RecdAmount = Header.RecdAmount + arInvRctLine.AdjustedAmount.GetValueOrDefault();

    }

    [RelayCommand]
    private void EvaluateArRctLine(InvoiceArReceipt arInvRctLine)
    {

        if (arInvRctLine.TransactionType is null)
        {
            return;
        }

        if (arInvRctLine.SeqNbr < 2)
        {
            arInvRctLine.BalBeforeAdj = Header.GrossRcbAmount.GetValueOrDefault();
            BalToAdjust = Header.GrossRcbAmount.GetValueOrDefault();
        };

        arInvRctLine.BalBeforeAdj = BalToAdjust;
        BalToAdjust = BalToAdjust - arInvRctLine.AdjustedAmount.GetValueOrDefault();
        arInvRctLine.BalanceAfterAdj = BalToAdjust;


        if (arInvRctLine.TransactionType is not null)
        {
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
        }

        Header.RecdAmount = Header.RecdAmount + arInvRctLine.AdjustedAmount.GetValueOrDefault();

    }

    [RelayCommand]
    private void EvaluateHeader()
    {
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

        //Header.AdvanceAdj.GetValueOrDefault() + Header.RdAmountAdj.GetValueOrDefault();
        //decimal AmountTobeDeducted = 0;
        //AmountTobeDeducted = Header.DiscountAmount.GetValueOrDefault() + Header.RoundOff.GetValueOrDefault();

        decimal payableValue = 0;
        payableValue = Header.GrossRcbAmount.GetValueOrDefault() -
                        Header.DiscountAmount.GetValueOrDefault();

        Header.AmountPayable = 0;
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

    private void ProcessInvBalance()
    {
        //Note if inv balance is greater than zero - we need to show message to get confirmation from user
        // and warn to check there is unpaid balance........ 

        if (Header.RecdAmount > 0)
        {
            if (Header.InvBalance > 0)
            {
                var result = _messageBoxService.ShowMessage("Received Amount is less than Invoice Amount, Do you want to make Credit for the balance Invoice Amount ?", "Invoice", MessageButton.YesNo, MessageIcon.Question, MessageResult.No);

                if (result == MessageResult.Yes)
                {
                    Header.PaymentDueDate = Header.InvDate.Value.AddDays(7);
                    Header.InvRefund = 0M;
                    BalanceVisible();
                    SetReceipts("Credit");
                }
                else
                {
                    return;
                }
            }
            else if (Header.InvBalance == 0)
            {
                Header.PaymentDueDate = null;
                BalanceVisible();

            }
            else if (Header.InvBalance < 0)
            {
                var result = _messageBoxService.ShowMessage("Received Amount is more than Invoice Amount, Do you want to Refund excess Amount ?", "Invoice", MessageButton.YesNo, MessageIcon.Question, MessageResult.No);

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
                    return;
                }
            }
        }
    }

    private void SetReceipts(String str)
    {

        InvoiceArReceipt arInvRct = new InvoiceArReceipt();

        arInvRct.InternalVoucherNbr = "Test";
        arInvRct.AdjustedAmount = Header.InvBalance;
        Header.ReceiptLines.Add(arInvRct);
    }

    private async void ProcessReceipts()
    {
        //For each Receipts row - seperate Voucher has to be created
        foreach(var receipts in Header.ReceiptLines)
        {
            var voucher = CreateVoucher(receipts);
            voucher = await SaveVoucher(voucher);

            var arReceipts = CreateArReceipts(receipts, voucher);
            await SaveArReceipts(arReceipts);

        }
    }

    private async void ProcessOldMetalTransaction()
    {
        //For each Receipts row - seperate Voucher has to be created
        foreach (var omTrans in Header.OldMetalTransactions)
        {
            var oldMTrans = CreateOldMetalTrans(omTrans);
            await SaveOldMetalTransaction(oldMTrans);

           // var arReceipts = CreateArReceipts(receipts, voucher);
           // await SaveArReceipts(arReceipts);

        }
    }

    private async void ProcessOldMetalTransLine()  //need to work out to add old metal totals to header old gold and silver amount
    {
        //For each Receipts row - seperate Voucher has to be created
        foreach (var oldMetalTrans in Header.OldMetalTransactions)
        {

            if (oldMetalTrans.Metal == "GOLD")
            {
                Header.OldGoldAmount = Header.OldGoldAmount + oldMetalTrans.FinalPurchasePrice;

            }
            else if (oldMetalTrans.Metal == "SILVER")
            {
                Header.OldSilverAmount = Header.OldSilverAmount + oldMetalTrans.FinalPurchasePrice;

            }

            //  var voucher = CreateVoucher(receipts);
            //  voucher = await SaveVoucher(voucher);

            //  var arReceipts = CreateArReceipts(receipts, voucher);
            //  await SaveArReceipts(arReceipts);

        }
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
        arInvRct.AdjustedAmount             = invoiceArReceipt.AdjustedAmount;
        arInvRct.InternalVoucherNbr         = voucher.VoucherNbr;
        arInvRct.InternalVoucherDate        = voucher.VoucherDate;
        arInvRct.Status = "Adj"; 

        return arInvRct;

    }

    private OldMetalTransaction CreateOldMetalTrans(OldMetalTransaction oldMetalTransaction)
    {

        OldMetalTransaction OldMetalTransaction = new()
        {
            TransDate = DateTime.Now
        };

        OldMetalTransaction.TransType = "Purchase";
        OldMetalTransaction.DocRefGkey = Header.GKey;
        OldMetalTransaction.DocRefNbr = Header.InvNbr;
        OldMetalTransaction.DocRefDate = Header.InvDate;
        OldMetalTransaction.CustGkey = Header.CustGkey;
        OldMetalTransaction.CustMobile = Header.CustMobile;

        return OldMetalTransaction;

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
        Voucher.VoucherType = "Sales";       // Voucher_type  1 = Sales,      2 = Credit,     3 = Expense
        Voucher.Mode = invoiceArReceipt.ModeOfReceipt; // Mode          1 = Cash,       2 = Bank,       3 = Credit
        Voucher.TransDate = Voucher.VoucherDate;    // DateTime.Now;
        Voucher.TransAmount = invoiceArReceipt.AdjustedAmount; // Header.RecdAmount;
        Voucher.VoucherNbr = "Sales-001";
        Voucher.RefDocNbr = Header.InvNbr;
        Voucher.RefDocDate = Header.InvDate;
        Voucher.RefDocGkey = Header.GKey;
        Voucher.TransDesc = Voucher.VoucherType + "-" + Voucher.TransType + "-" + Voucher.Mode;

        return Voucher;

    }

    private async Task SaveOldMetalTransaction(OldMetalTransaction oldMetalTransaction)
    {
        if (oldMetalTransaction.GKey == 0)
        {
            try
            {
                var oldMetalTransResult = await _oldMetalTransactionService.CreatOldMetalTransaction(oldMetalTransaction);

                if (oldMetalTransResult != null)
                {
                    oldMetalTransaction = oldMetalTransResult;
                }

            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }

        }
        else
        {
            await _oldMetalTransactionService.UpdateOldMetalTransaction(oldMetalTransaction);
        }

    }

    private async Task SaveArReceipts(InvoiceArReceipt invoiceArReceipt)
    {
        if (invoiceArReceipt.GKey == 0)
        {
            try
            {
                var voucherResult = await _invoiceArReceiptService.CreatInvArReceipt(invoiceArReceipt);

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
            var voucherResult = await _voucherService.CreatVoucher(voucher);

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
       // var result = _messageBoxService.ShowMessage("Reset all values", "Reset Invoice", MessageButton.YesNo, MessageIcon.Question, MessageResult.No);

       // if (result == MessageResult.No)
       //     return;

        SetHeader();
        Buyer = null;
        CustomerPhoneNumber = null;
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
            GstLocSeller = "33"
        };
    }

    private decimal GetGSTWithinState()
    {
        if (Buyer?.GstStateCode == "33")
        {
            return Math.Round(SCGSTPercent / 2, 3);
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
}