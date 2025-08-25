using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DevExpress.Mvvm;
using DevExpress.Mvvm.Native;
using DevExpress.Xpf.Grid;
using DevExpress.Xpf.Core;
using InvEntry.Extension;
using InvEntry.Models;
using InvEntry.Services;
using InvEntry.Utils.Options;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

using IDialogService = DevExpress.Mvvm.IDialogService;
using System.ComponentModel;

namespace InvEntry.ViewModels;

public partial class InvoiceWithVouchersViewModel : ObservableObject
{
    [ObservableProperty]
    public ObservableCollection<Customer> _osCustomers;

    [ObservableProperty]
    public ObservableCollection<InvoiceLine> _invoiceItems;

    [ObservableProperty]
    public ObservableCollection<InvoiceArReceipt> _invReceipts;

    [ObservableProperty]
    private ObservableCollection<InvoiceHeader> _osInvoices;

    //[ObservableProperty]
    //private ObservableCollection<MtblReference> _mtblReferencesList;

    [ObservableProperty]
    private ObservableCollection<string> paymentModeList;

    [ObservableProperty]
    private Customer _selectedCustomer;

    [ObservableProperty]
    private InvoiceHeader _selectedInvoice;

    [ObservableProperty]
    private DateTime invoiceDate = DateTime.Now;

    [ObservableProperty]
    private DateSearchOption _searchOption;

    [ObservableProperty]
    private string selectedPaymentType;

    [ObservableProperty]
    private decimal _adjustmentAmount;

    [ObservableProperty]
    private decimal customerOutstanding;

    [ObservableProperty]
    private DateTime _paidOn = DateTime.Today;

    [ObservableProperty]
    private DateTime _Today = DateTime.Today;

    private readonly ICustomerService _customerService;
    private readonly IInvoiceService _invoiceService;
    private readonly IVoucherService _voucherService;
    private readonly IInvoiceArReceiptService _invoiceArReceiptService;
    private readonly IMtblReferencesService _mtblReferencesService;
    private readonly IMessageBoxService _messageBoxService;
    private readonly IDialogService _reportDialogService;

    public decimal TotalInvoiceAmount => (decimal)OsInvoices.Sum(i => i.GrossRcbAmount);
    public decimal TotalVoucherAmount => (decimal)OsInvoices.Sum(v => v.RecdAmount);
    public decimal BalanceAmount => (decimal)OsInvoices.Sum(v => v.InvBalance);

    //public List<InvoiceLine> Items => InvoiceItems.ToList();
    public List<string> mobileNbrs = [];

    public ICommand SaveCommand { get; }

    public InvoiceWithVouchersViewModel(IInvoiceService invoiceService,
                                        ICustomerService customerService,
                                        IVoucherService voucherService,
                                        IMtblReferencesService mtblReferencesService,
                                        IInvoiceArReceiptService invoiceArReceiptService,
                                        IMessageBoxService messageBoxService,
                                        [FromKeyedServices("ReportDialogService")] IDialogService reportDialogService)
    {
        //to do  Customers = CustomerService.LoadCustomers();

        _customerService = customerService;
        _invoiceService = invoiceService;
        _voucherService = voucherService;
        _invoiceArReceiptService = invoiceArReceiptService;
        _mtblReferencesService = mtblReferencesService;
        _messageBoxService = messageBoxService;
        _reportDialogService = reportDialogService;

        _searchOption = new();
        SearchOption.To = Today;
        SearchOption.From = Today.AddDays(-1);
        SearchOption.Filter1 = null;

        OsInvoices = new();
        mobileNbrs = new();
        OsCustomers = new();

        PopulateMtblRefNameList();

        Task.Run(RefreshInvoicesAsync).Wait();

        //   SaveCommand = new AsyncRelayCommand(SaveAsync);

        //    PropertyChanged += (_, e) =>
        //    {
        //       SearchOption.To = Today;
        //       SearchOption.From = Today.AddDays(-1);
        //        SearchOption.Filter1 = null; // SelectedCustomer.MobileNbr;

        //        Task.Run(RefreshInvoicesAsync).Wait();

        //if (e.PropertyName == nameof(SelectedCustomer))
        //   LoadOutstanding();

        //       OnPropertyChanged(nameof(TotalInvoiceAmount));
        //        OnPropertyChanged(nameof(TotalVoucherAmount));
        //        OnPropertyChanged(nameof(BalanceAmount));
        //    };
    }

    private async void PopulateMtblRefNameList()
    {
        var mtblRefList = await _mtblReferencesService.GetReferenceList("PAYMENT_MODE");

        PaymentModeList = new(mtblRefList
                                .Select(x => x.RefValue)
                                .OrderBy(x => x)
                             );
    }


    [RelayCommand]
    private async Task RefreshInvoicesAsync()
    {
        OsInvoices.Clear();
        mobileNbrs.Clear();

        var waitVM = WaitIndicatorVM.ShowIndicator("Please wait.... processing your request.... .");

        SplashScreenManager.CreateWaitIndicator(waitVM).Show();

        var OsInvlist = await _invoiceService.GetOutStanding(SearchOption);

        if (OsInvlist is not null)
        {
            foreach (var invoice in OsInvlist)
            {
                OsInvoices.Add(invoice);
                mobileNbrs.Add(invoice.CustMobile);
            }
        }

        if (mobileNbrs.Count > 0)
        {
            OsCustomers.Clear();
            var cust = await _customerService.GetCustomers(mobileNbrs);

            foreach (var c in cust)
            {
                OsCustomers.Add(c);
            }
        }

        SplashScreenManager.ActiveSplashScreens.FirstOrDefault(x => x.ViewModel == waitVM).Close();
    }

    [RelayCommand]
    private async Task SelectionInvoiceChanged()
    {
        if (SelectedInvoice is null) return;

        var arReceiptsList = await _invoiceArReceiptService.GetByInvHdrGKey(SelectedInvoice.GKey);

        if (arReceiptsList is not null)
            SelectedInvoice.ReceiptLines = new(arReceiptsList);

        arReceiptsList = [.. arReceiptsList.OrderBy(c => c.GKey)];

        SelectedCustomer = await _customerService.GetCustomer(SelectedInvoice.CustMobile);

        foreach (var cust in OsCustomers)
        {
            if (cust.MobileNbr == SelectedInvoice.CustMobile)
            {
                SelectedCustomer = cust;
            }
        }
    }

    [RelayCommand]
    private void CellUpdate(CellValueChangedEventArgs args)
    {

        if (args.Row is InvoiceArReceipt arInvRctline)
        {
            EvaluateArRctLine(arInvRctline);
        }

    }

    private void EvaluateArRctLine(InvoiceArReceipt arInvRctLine)
    {

        if (string.IsNullOrEmpty(arInvRctLine.TransactionType))
        {
            return;
        }

        if (!arInvRctLine.BalBeforeAdj.HasValue)
            arInvRctLine.BalBeforeAdj = SelectedInvoice.InvBalance.GetValueOrDefault();

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

        //  EvaluateHeader();

    }

    [RelayCommand]
    private async Task ApplyAdjustment()
    {

        if (AdjustmentAmount > 0)
        {

            //accepting single row from user, create invoice receipts based on this single row
            //For each Receipts row - seperate Voucher has to be created

            var voucher = CreateVoucher();
            voucher = await SaveVoucher(voucher);

            var arReceipts = CreateArReceipts(voucher);

            InvBalanceCheck();

            EvaluateArRctLine(arReceipts);

            await SaveArReceipts(arReceipts);

            await UpdateInvoice(SelectedInvoice);

        }
        else
        {
            _messageBoxService.ShowMessage("Invalid Amount.", "Invalid AdjustmentAmount",
                            MessageButton.OK, MessageIcon.None);
            return;
        }

        ResetAdjPanel();
        await SelectionInvoiceChanged();

        _messageBoxService.ShowMessage("Sucessfully adjusted against outstanding Invoice amount.", "Outstanding Adjustment",
                                    MessageButton.OK, MessageIcon.None);
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

    [RelayCommand] //(CanExecute = nameof(CanPrintInvoice))]
    private void PrintInvoice()
    {
        _reportDialogService.PrintPreview(SelectedInvoice.InvNbr);

    }

    [RelayCommand]
    private void SelectionChanged()
    {
        PrintInvoiceCommand.NotifyCanExecuteChanged();
    }

    private void InvBalanceCheck()
    {

        if (SelectedInvoice == null) return;

        if (SelectedInvoice.InvBalance <= 0)
            //show message and return
            return;

        //partial and full payment
        if (SelectedInvoice.InvBalance.GetValueOrDefault() >= AdjustmentAmount)
            SelectedInvoice.InvBalance = SelectedInvoice.InvBalance.GetValueOrDefault() - AdjustmentAmount;

        else if (SelectedInvoice.InvBalance.GetValueOrDefault() < AdjustmentAmount)
        {
            SelectedInvoice.InvBalance = 0;
            AdjustmentAmount = AdjustmentAmount - SelectedInvoice.InvBalance.GetValueOrDefault();

            var result = _messageBoxService.ShowMessage(
                                                            "Received Amount is more than the Invoice Amount, " +
                                                            "Excess Amount of Rs. " +
                                                            AdjustmentAmount + " ?", "Invoice",
                                                            MessageButton.YesNo,
                                                            MessageIcon.Question,
                                                            MessageResult.No
                                                       );

            if (result == MessageResult.Yes)
            {
            }



            //in this sceneario - supposed to create two voucher - one is for adjusting the invoice balance and another is for advance

        }

    }

    // private bool ProcessInvBalance()
    // {

    // ProcessSettlements();

    //Note if inv balance is greater than zero - we need to show message to get confirmation from user
    // and warn to check there is unpaid balance........ 

    //        if (Header.InvBalance > 0)
    /*        {
                var result = _messageBoxService.ShowMessage("Received Amount is lesser than the Invoice Amount, " +
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
                {*/
    /*                Header.PaymentDueDate = null;
                    Header.InvRefund = Header.InvBalance * -1;
                    Header.InvBalance = 0M;
                    RefundVisible();
                    SetReceipts("Refund");*/
    /*            }
                else
                {
                    return false;
                }

            }

            return true;
        }*/
    ////////////////////

    private async Task SaveAsync()
    {
        if (CustomerOutstanding > 0)
        {
            var confirm = MessageBox.Show(
                $"Customer has ₹{CustomerOutstanding} due. Continue?",
                "Outstanding Alert", MessageBoxButton.YesNo);

            if (confirm != MessageBoxResult.Yes)
                return;
        }

        var invoice = new InvoiceHeader
        {
            CustMobile = SelectedCustomer.CustomerName,
            InvDate = InvoiceDate,
            //    GrossRcbAmount = TotalInvoiceAmount,
            //    Lines = InvoiceItems
        };

        /*        var vouchers = VoucherEntries.Select(v => new Voucher
                {
                    VoucherType = v.VoucherType,
                    TransAmount = v.TransAmount,
                    TransDesc = v.TransDesc,
                    VoucherDate = DateTime.Now,
                    CustomerGkey = SelectedCustomer.GKey
                }).ToList();*/

        // await InvoiceService.SaveInvoiceWithVouchersAsync(invoice, vouchers);

        MessageBox.Show("Invoice and vouchers saved.");
        InvoiceItems.Clear();
        //VoucherEntries.Clear();
    }

    private Voucher CreateVoucher()
    {

        Voucher Voucher = new()
        {
            VoucherDate = DateTime.Now
        };

        Voucher.SeqNbr = 1;
        Voucher.CustomerGkey = SelectedInvoice.CustGkey;
        Voucher.VoucherDate = DateTime.Now;
        Voucher.TransType = "Receipt";         // Trans_type    1 = Receipt,    2 = Payment,    3 = Journal
        Voucher.VoucherType = SelectedPaymentType; // invoiceArReceipt.TransactionType; // Voucher_type  1 = Sales,      2 = Credit,     3 = Expense
        Voucher.Mode = "Cash"; // Mode          1 = Cash,       2 = Bank,       3 = Credit
        Voucher.TransDate = PaidOn; // DateTime.Now;  
        Voucher.VoucherNbr = SelectedInvoice.InvNbr;
        Voucher.RefDocNbr = SelectedInvoice.InvNbr;
        Voucher.RefDocDate = SelectedInvoice.InvDate;
        Voucher.RefDocGkey = SelectedInvoice.GKey;
        Voucher.TransDesc = Voucher.VoucherType + "-" + Voucher.TransType + "-" + Voucher.Mode;
        Voucher.TransAmount = AdjustmentAmount;

        return Voucher;

    }

    private async Task<Voucher> SaveVoucher(Voucher voucher)
    {
        if (voucher.GKey == 0)
        {
            var voucherResult = await _voucherService.CreateVoucher(voucher);

            if (voucherResult != null)
            {
                voucher = voucherResult;

            }
        }
        else
        {
            await _voucherService.UpdateVoucher(voucher);
        }

        return voucher;

    }

    private InvoiceArReceipt CreateArReceipts(Voucher voucher)
    {

        InvoiceArReceipt arInvRct = new()
        {
            //VoucherDate = DateTime.Now
        };

        arInvRct.SeqNbr = SelectedInvoice.ReceiptLines.Count + 1;
        arInvRct.CustGkey = SelectedInvoice.CustGkey;
        arInvRct.InvoiceGkey = (int?)SelectedInvoice.GKey;
        arInvRct.InvoiceNbr = SelectedInvoice.InvNbr;
        arInvRct.InvoiceReceivableAmount = SelectedInvoice.InvBalance;
        arInvRct.BalanceAfterAdj = SelectedInvoice.InvBalance - AdjustmentAmount;
        arInvRct.TransactionType = SelectedPaymentType; // invoiceArReceipt.TransactionType;
                                                        // arInvRct.ModeOfReceipt = invoiceArReceipt.ModeOfReceipt;
        arInvRct.BalBeforeAdj = SelectedInvoice.InvBalance;
        arInvRct.InternalVoucherNbr = voucher.VoucherNbr;
        arInvRct.InternalVoucherDate = voucher.VoucherDate;
        arInvRct.InvoiceReceiptNbr = SelectedInvoice.InvNbr.Replace("B", "R");  //hard coded - future review 
        arInvRct.Status = "Adj";

        //var adjustedAmount = AdjustmentAmount; // getTransAmount(invoiceArReceipt.TransactionType);
        arInvRct.AdjustedAmount = AdjustmentAmount; ;
        //adjustedAmount == 0 ? invoiceArReceipt.AdjustedAmount : adjustedAmount;

        return arInvRct;

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

    private async Task UpdateInvoice(InvoiceHeader Invoice)
    {
        await _invoiceService.UpdateHeader(Invoice);
    }

    private void ResetAdjPanel()
    {

        AdjustmentAmount = 0;
        SelectedPaymentType = null;

        /*        SetHeader();
                SetThisCompany();
                //SetMasterLedger();
                Buyer = null;
                CustomerPhoneNumber = null;
                CustomerState = null;
                SalesPerson = null;
                CreateInvoiceCommand.NotifyCanExecuteChanged();
                invBalanceChk = false;  //reset to false for next invoice*/
    }

}
