using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DevExpress.Mvvm;
using DevExpress.Mvvm.Native;
using DevExpress.Xpf.Core;
using DevExpress.Xpf.Editors;
using DevExpress.Xpf.Grid;
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

public partial class EstimateViewModel: ObservableObject
{
    [ObservableProperty]
    private string _customerPhoneNumber;

    [ObservableProperty]
    private MtblReference _customerState;

    [ObservableProperty]
    private Customer _buyer;

    [ObservableProperty]
    private EstimateHeader _header;

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
    private ObservableCollection<EstimateLine> selectedRows;

    [ObservableProperty]
    private ObservableCollection<string> productCategoryList;

    [ObservableProperty]
    private ObservableCollection<string> metalList;

    [ObservableProperty]
    private ObservableCollection<MtblReference> mtblReferencesList;

    [ObservableProperty]
    private ObservableCollection<MtblReference> stateReferencesList;

    public ICommand ShowWindowCommand { get; set; }

    private bool createCustomer = false;
    private bool estBalanceChk = false;

    private readonly ICustomerService _customerService;
    private readonly IProductService _productService;
    private readonly IDialogService _dialogService;
    private readonly IDialogService _reportDialogService;
    private readonly IMessageBoxService _messageBoxService;
    private readonly IVoucherService _voucherService;
    private readonly IEstimateService _estimateService;
    private readonly IProductCategoryService _productCategoryService;
    private readonly IMtblReferencesService _mtblReferencesService;
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
        IProductService productService,
        IDialogService dialogService,
        IEstimateService estimateService,
        IProductCategoryService productCategoryService,
        IMessageBoxService messageBoxService,
        IMtblReferencesService mtblReferencesService,
        SettingsPageViewModel settingsPageViewModel,
        IReportFactoryService reportFactoryService,
        [FromKeyedServices("ReportDialogService")] IDialogService reportDialogService)
    {

        //MtblRefNameList = new();

        SetHeader();
        _customerService = customerService;
        _productService = productService;
        _productCategoryService = productCategoryService;
        _dialogService = dialogService;
        _estimateService = estimateService;
        _messageBoxService = messageBoxService;
        _reportDialogService = reportDialogService;
        _reportFactoryService = reportFactoryService;
        _mtblReferencesService = mtblReferencesService;

        selectedRows = new();
        _customerReadOnly = false;

        _isBalance = true;
        _isRefund = false;
        _settingsPageViewModel = settingsPageViewModel;

        PopulateProductCategoryList();
        PopulateStateList();
        PopulateUnboundLineDataMap();
        PopulateMtblRefNameList();
        PopulateMetalList();
    }

    private async void PopulateProductCategoryList()
    {
        var list = await _productCategoryService.GetProductCategoryList();
        ProductCategoryList = new(list.Select(x => x.Name));
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
        if (copyEstimateExpression is null) copyEstimateExpression = new();

        copyEstimateExpression.Add($"{nameof(EstimateLine.EstlTaxableAmount)}", (item, val) => item.EstlTaxableAmount = val);
        copyEstimateExpression.Add($"{nameof(EstimateLine.ProdNetWeight)}", (item, val) => item.ProdNetWeight = val);
        copyEstimateExpression.Add($"{nameof(EstimateLine.EstlGrossAmt)}", (item, val) => item.EstlGrossAmt = val);
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
        if (Buyer is null) return;

        Buyer.GstStateCode = value.RefCode;    //Need to fetch based on pincode - future change
        Header.GstLocBuyer = value.RefCode;
        Header.CgstPercent = GetGSTWithinState();
        Header.SgstPercent = GetGSTWithinState();
        Header.IgstPercent = IGSTPercent;
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

        EstimateLine estimateLine = new EstimateLine()
        {
            ProdQty = 1,
            EstlBilledPrice = billedPrice,
            EstlCgstPercent = GetGSTWithinState(),
            EstlSgstPercent = GetGSTWithinState(),
            EstlIgstPercent = IGSTPercent,
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

    /*    [RelayCommand]
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
            };

            Header.OldMetalTransactions.Add(oldMetalTransactionLine);
            return Task.CompletedTask;
        }*/

    /*    [RelayCommand(CanExecute = nameof(CanProcessArReceipts))]
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

        }*/

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
        estBalanceChk = true;  //is this a right place to fix
        //ProcessEstBalance();

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
            Buyer = await _customerService.CreatCustomer(Buyer);
        }

        //Header.InvNbr = InvoiceNumberGenerator.Generate();
        Header.CustGkey = (int?)Buyer.GKey;

        Header.Lines.ForEach(x =>
        {
            x.EstLineNbr = Header.Lines.IndexOf(x) + 1;
            x.EstId = Header.EstNbr;
        });

        var header = await _estimateService.CreatHeader(Header);

        if (header is not null)
        {
            Header.GKey = header.GKey;
            Header.EstNbr = header.EstNbr;
            Header.Lines.ForEach(x =>
            {
                x.EstHdrGkey = header.GKey;
                x.EstId = header.EstNbr;
            });
            // loop for validation check for customer
            await _estimateService.CreateEstimateLine(Header.Lines);

            //   await ProcessOldMetalTransaction();

            //Invoice header details needs to be saved alongwith receipts, hence calling from here.
            //    ProcessReceipts();

            _messageBoxService.ShowMessage("Estimate Created Successfully", "Estimate Created", MessageButton.OK, MessageIcon.Exclamation);

            Messenger.Default.Send(MessageType.WaitIndicator, WaitIndicatorVM.ShowIndicator("Print Estimate..."));
            PrintPreviewEstimate();
            PrintPreviewEstimateCommand.NotifyCanExecuteChanged();
            PrintEstimateCommand.NotifyCanExecuteChanged();
            Messenger.Default.Send(MessageType.WaitIndicator, WaitIndicatorVM.HideIndicator());

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
        //    var printed = PrintHelper.Print(_reportFactoryService.CreateEstimateReport(Header.EstNbr));
        //
        //    if (printed.HasValue && printed.Value)
        //        _messageBoxService.ShowMessage("Estimate printed Successfully", "Estimatee print", MessageButton.OK, MessageIcon.None);
    }

    private bool CanPrintEstimate()
    {
        return !CanCreateEstimate();
    }

    [RelayCommand(CanExecute = nameof(CanPrintEstimate))]
    private void PrintPreviewEstimate()
    {
        _reportDialogService.PrintPreview(Header.EstNbr);
        ResetEstimate();
    }

    [RelayCommand(CanExecute = nameof(CanPrintEstimate))]
    private void ExportToPdf()
    {
        //after report creation uncomment this
        //    _reportFactoryService.CreateEstimateReportPdf(Header.EstNbr, "C:\\Madrone\\Invoice\\");
    }

    [RelayCommand]
    private void CellUpdate(CellValueChangedEventArgs args)
    {
        if (args.Row is EstimateLine line)
        {
            EvaluateFormula(line);
        }
        /*        else
                if (args.Row is EstimateArReceipt arInvRctline)
                {
                    EvaluateArRctLine(arInvRctline);
                }
                else
                if (args.Row is OldMetalTransaction oldMetalTransaction &&
                    args.Column.FieldName != nameof(OldMetalTransaction.FinalPurchasePrice))
                {
                    EvaluateOldMetalTransactions(oldMetalTransaction);
                }*/

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

    /*    [RelayCommand]
        private void EvaluateArRctLine(InvoiceArReceipt arInvRctLine)
        {

            if (string.IsNullOrEmpty(arInvRctLine.TransactionType))
            {
                return;
            }

            if (!arInvRctLine.BalBeforeAdj.HasValue)
                arInvRctLine.BalBeforeAdj = Header.InvBalance.GetValueOrDefault();

            arInvRctLine.BalanceAfterAdj = arInvRctLine.BalBeforeAdj.GetValueOrDefault() - arInvRctLine.AdjustedAmount.GetValueOrDefault();


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

        }*/

    /*    private decimal FilterReceiptTransactions(string transType)
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
        }*/

    [RelayCommand]
    private void EvaluateHeader()
    {

        // Header.AdvanceAdj = FilterReceiptTransactions("Advance");
        // Header.RdAmountAdj = FilterReceiptTransactions("RD");

        Header.RecdAmount = 0; // Header.ReceiptLines.Select(x => x.AdjustedAmount).Sum();

        Header.OldGoldAmount = 0; // FilterMetalTransactions("OLD GOLD");

        Header.OldSilverAmount = 0; // FilterMetalTransactions("OLD SILVER");

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

    /*    private void ProcessEstBalance()
        {
            //Note if inv balance is greater than zero - we need to show message to get confirmation from user
            // and warn to check there is unpaid balance........ 

            //    if (Header.RecdAmount > 0)
            //    {
            if (Header.EstBalance > 0)
            {
                var result = _messageBoxService.ShowMessage("Received Amount is less than Estimate Amount, Do you want to make Credit for the balance Estoice Amount ?", "Estoice", MessageButton.YesNo, MessageIcon.Question, MessageResult.No);

                if (result == MessageResult.Yes)
                {
                    Header.PaymentDueDate = Header.EstDate.Value.AddDays(7);
                    Header.EstRefund = 0M;
                    BalanceVisible();
                    SetReceipts("Credit");
                }
                else
                {
                    return;
                }
            }
            else if (Header.EstBalance == 0)
            {
                Header.PaymentDueDate = null;
                BalanceVisible();

            }
            else if (Header.EstBalance < 0)
            {
                var result = _messageBoxService.ShowMessage("Received Amount is more than Estoice Amount, Do you want to Refund excess Amount ?", "Estoice", MessageButton.YesNo, MessageIcon.Question, MessageResult.No);

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
                    return;
                }
                //        }
            }
        }*/

    /*    private void SetReceipts(String str)
        {

            InvoiceArReceipt arInvRct = new InvoiceArReceipt();

            arInvRct.InternalVoucherNbr = "Test";
            arInvRct.AdjustedAmount = Header.InvBalance;
            Header.ReceiptLines.Add(arInvRct);
        }

        private async void ProcessReceipts()
        {
            //For each Receipts row - seperate Voucher has to be created
            foreach (var receipts in Header.ReceiptLines)
            {
                var voucher = CreateVoucher(receipts);
                voucher = await SaveVoucher(voucher);

                var arReceipts = CreateArReceipts(receipts, voucher);
                await SaveArReceipts(arReceipts);

            }
        }*/

    /*    private async Task ProcessOldMetalTransaction()
        {
            foreach (var omTrans in Header.OldMetalTransactions)
            {
                omTrans.EnrichHeaderDetails(Header);
            }

            await _oldMetalTransactionService.CreatOldMetalTransaction(Header.OldMetalTransactions);
        }*/

    /*    private async void ProcessOldMetalTransLine()  //need to work out to add old metal totals to header old gold and silver amount
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

            }
        }*/

    /*    private InvoiceArReceipt CreateArReceipts(InvoiceArReceipt invoiceArReceipt, Voucher voucher)
        {

            InvoiceArReceipt arInvRct = new()
            {
                //VoucherDate = DateTime.Now
            };

            arInvRct.SeqNbr = invoiceArReceipt.SeqNbr;
            arInvRct.CustGkey = invoiceArReceipt.CustGkey;
            arInvRct.InvoiceGkey = (int?)Header.GKey;
            arInvRct.InvoiceNbr = Header.InvNbr;
            arInvRct.InvoiceReceivableAmount = invoiceArReceipt.InvoiceReceivableAmount;
            arInvRct.BalanceAfterAdj = invoiceArReceipt.BalanceAfterAdj;
            arInvRct.TransactionType = invoiceArReceipt.TransactionType;
            arInvRct.ModeOfReceipt = invoiceArReceipt.ModeOfReceipt;
            arInvRct.BalBeforeAdj = invoiceArReceipt.BalBeforeAdj;
            arInvRct.AdjustedAmount = invoiceArReceipt.AdjustedAmount;
            arInvRct.InternalVoucherNbr = voucher.VoucherNbr;
            arInvRct.InternalVoucherDate = voucher.VoucherDate;
            arInvRct.Status = "Adj";

            return arInvRct;

        }*/

    /*    private Voucher CreateVoucher(InvoiceArReceipt invoiceArReceipt)
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

        }*/



    /*    private async Task SaveArReceipts(InvoiceArReceipt invoiceArReceipt)
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

        }*/

    /*    //[RelayCommand]
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

        }*/

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
        Buyer = null;
        CustomerPhoneNumber = null;
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
