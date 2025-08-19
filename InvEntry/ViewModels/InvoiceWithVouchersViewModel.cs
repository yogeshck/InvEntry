using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DevExpress.Mvvm.Native;
using DevExpress.Xpf.Grid;
using InvEntry.Models;
using InvEntry.Services;
using InvEntry.Utils.Options;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

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
    private decimal adjustmentAmount;

    [ObservableProperty]
    private decimal customerOutstanding;

    [ObservableProperty]
    private DateTime _Today = DateTime.Today;

    private readonly ICustomerService _customerService;
    private readonly IInvoiceService _invoiceService;
    private readonly IVoucherService _voucherService;
    private readonly IInvoiceArReceiptService _invoiceArReceiptService;
    private readonly IMtblReferencesService _mtblReferencesService;

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
                                        IInvoiceArReceiptService invoiceArReceiptService)
    {
        //to do  Customers = CustomerService.LoadCustomers();

        _customerService = customerService;
        _invoiceService = invoiceService;
        _voucherService = voucherService;
        _invoiceArReceiptService = invoiceArReceiptService;
        _mtblReferencesService = mtblReferencesService;

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
        PaymentModeList = new(mtblRefList.Select(x => x.RefValue));
    }


    [RelayCommand]
    private async Task RefreshInvoicesAsync()
    {
        OsInvoices.Clear();
        mobileNbrs.Clear();

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

    }

    [RelayCommand]
    private async Task SelectedARInvoice()
    {
        var invoiceARList = await _invoiceArReceiptService.GetByInvHdrGKey(SelectedInvoice.GKey);

        // var grnHeader = await _grnService.GetByHdrGkey(SelectedGrn.GKey);

    }

    [RelayCommand]
    private async Task SelectionInvoiceChanged()
    {
        if (SelectedInvoice is null) return;

           var arReceiptsList = await _invoiceArReceiptService.GetByInvHdrGKey(SelectedInvoice.GKey);

              if (arReceiptsList is not null)
        //InvReceipts
                  SelectedInvoice.ReceiptLines = new(arReceiptsList);

        SelectedCustomer = await _customerService.GetCustomer(SelectedInvoice.CustMobile);

        foreach (var cust in OsCustomers)
        {
            if (cust.MobileNbr == SelectedInvoice.CustMobile)
            {
                SelectedCustomer = cust;
            }
        }
    }

    [RelayCommand] //(CanExecute = nameof(CanProcessArReceipts))]
    private async Task ProcessArReceipts()
    {
        //  var paymentMode = await _mtblReferencesService.GetReference("PAYMENT_MODE");
        // _productService.GetProduct(ProductIdUI);

        // var waitVM = WaitIndicatorVM.ShowIndicator("Fetching Invoice Receipt details...");

        var noOfLines = SelectedInvoice.ReceiptLines.Count;

        InvoiceArReceipt arInvRctLine = new InvoiceArReceipt()
        {
            InvoiceGkey = SelectedInvoice.GKey,
            CustGkey = SelectedInvoice.CustGkey,
            Status = "Open",    //Status Open - Before Adjustment
            SeqNbr = noOfLines + 1
        };

        SelectedInvoice.ReceiptLines.Add(arInvRctLine);

    }

    private bool CanProcessArReceipts()
    {
        return !SelectedInvoice.InvBalance.HasValue || SelectedInvoice.InvBalance > 0;

    }

    [RelayCommand]
    private void CellUpdate(CellValueChangedEventArgs args)
    {

        if (args.Row is InvoiceArReceipt arInvRctline)
        {
            EvaluateArRctLine(arInvRctline);
        }

    }

    /*    private bool CanDeleteRows()
        {
            return SelectedRows?.Any() ?? false;
        }*/

    /*    [RelayCommand(CanExecute = nameof(CanDeleteSingleRow))]
        private void DeleteSingleRow(InvoiceLine line)
        {
            var result = _messageBoxService.ShowMessage("Delete current row", "Delete Row", MessageButton.YesNo, MessageIcon.Question, MessageResult.No);

            if (result == MessageResult.No)
                return;

            var index = InvReceipts.Remove(line);
        }

        private bool CanDeleteSingleRow(InvoiceLine line)
        {
            return line is not null && InvReceipts.IndexOf(line) > -1;
        }*/

    [RelayCommand]
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

        EvaluateArRctLine(SelectedInvoice.ReceiptLines);

        //For each Receipts row - seperate Voucher has to be created
        foreach (var receipts in SelectedInvoice.ReceiptLines)
        {
            if (receipts is null) return;

            var voucher = CreateVoucher(receipts);
            voucher = await SaveVoucher(voucher);

            var arReceipts = CreateArReceipts(receipts, voucher);
            await SaveArReceipts(arReceipts);

        }

    }

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

    private Voucher CreateVoucher(InvoiceArReceipt invoiceArReceipt)
    {

        Voucher Voucher = new()
        {
            VoucherDate = DateTime.Now
        };

        Voucher.SeqNbr = 1;
        Voucher.CustomerGkey = SelectedInvoice.CustGkey;
        Voucher.VoucherDate = DateTime.Now
        Voucher.TransType = "Receipt";         // Trans_type    1 = Receipt,    2 = Payment,    3 = Journal
        Voucher.VoucherType = SelectedPaymentType; // invoiceArReceipt.TransactionType; // Voucher_type  1 = Sales,      2 = Credit,     3 = Expense
        Voucher.Mode = invoiceArReceipt.ModeOfReceipt; // Mode          1 = Cash,       2 = Bank,       3 = Credit
        Voucher.TransDate = DateTime.Now;  
        Voucher.VoucherNbr = SelectedInvoice.InvNbr;
        Voucher.RefDocNbr = SelectedInvoice.InvNbr;
        Voucher.RefDocDate = SelectedInvoice.InvDate;
        Voucher.RefDocGkey = SelectedInvoice.GKey;
        Voucher.TransDesc = Voucher.VoucherType + "-" + Voucher.TransType + "-" + Voucher.Mode;

        var transAmount = AdjustmentAmount; // getTransAmount(invoiceArReceipt.TransactionType);
        Voucher.TransAmount = transAmount == 0 ? invoiceArReceipt.AdjustedAmount : transAmount;

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

    private InvoiceArReceipt CreateArReceipts(InvoiceArReceipt invoiceArReceipt, Voucher voucher)
    {

        InvoiceArReceipt arInvRct = new()
        {
            //VoucherDate = DateTime.Now
        };

        arInvRct.SeqNbr = invoiceArReceipt.SeqNbr;
        arInvRct.CustGkey = invoiceArReceipt.CustGkey;
        arInvRct.InvoiceGkey = (int?)SelectedInvoice.GKey;
        arInvRct.InvoiceNbr = SelectedInvoice.InvNbr;
        arInvRct.InvoiceReceivableAmount = invoiceArReceipt.InvoiceReceivableAmount;
        arInvRct.BalanceAfterAdj = invoiceArReceipt.BalanceAfterAdj;
        arInvRct.TransactionType = SelectedPaymentType; // invoiceArReceipt.TransactionType;
        arInvRct.ModeOfReceipt = invoiceArReceipt.ModeOfReceipt;
        arInvRct.BalBeforeAdj = invoiceArReceipt.BalBeforeAdj;
        arInvRct.InternalVoucherNbr = voucher.VoucherNbr;
        arInvRct.InternalVoucherDate = voucher.VoucherDate;
        arInvRct.InvoiceReceiptNbr = SelectedInvoice.InvNbr.Replace("B", "R");  //hard coded - future review 
        arInvRct.Status = "Adj";

        var adjustedAmount = AdjustmentAmount; // getTransAmount(invoiceArReceipt.TransactionType);
        arInvRct.AdjustedAmount = adjustedAmount == 0 ? invoiceArReceipt.AdjustedAmount : adjustedAmount;

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

    private void ResetAdjPanel()
    {
        SetHeader();
        SetThisCompany();
        //SetMasterLedger();
        Buyer = null;
        CustomerPhoneNumber = null;
        CustomerState = null;
        SalesPerson = null;
        CreateInvoiceCommand.NotifyCanExecuteChanged();
        invBalanceChk = false;  //reset to false for next invoice
    }

}
