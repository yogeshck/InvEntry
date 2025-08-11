using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using InvEntry.Models;
using InvEntry.Services;
using InvEntry.Utils.Options;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace InvEntry.ViewModels;

public partial class InvoiceWithVouchersViewModel : ObservableObject
{
    [ObservableProperty]
    public ObservableCollection<Customer> _customers;

    [ObservableProperty]
    public ObservableCollection<InvoiceLine> _invoiceItems;

    [ObservableProperty]
    public ObservableCollection<InvoiceArReceipt> _invReceipts;

    [ObservableProperty]
    private ObservableCollection<InvoiceHeader> _osInvoices;

    [ObservableProperty] 
    private Customer selectedCustomer;

    [ObservableProperty]
    private InvoiceHeader _selectedInvoice;

    [ObservableProperty] 
    private DateTime invoiceDate = DateTime.Now;

    [ObservableProperty]
    private DateSearchOption _searchOption;

    [ObservableProperty] 
    private decimal customerOutstanding;

    [ObservableProperty]
    private DateTime _Today = DateTime.Today;

    private readonly ICustomerService _customerService;
    private readonly IInvoiceService _invoiceService;
    private readonly IInvoiceArReceiptService _invoiceArReceiptService;

    public decimal TotalInvoiceAmount => (decimal)OsInvoices.Sum(i => i.GrossRcbAmount);
    public decimal TotalVoucherAmount => (decimal)OsInvoices.Sum(v => v.RecdAmount);
    public decimal BalanceAmount => (decimal)OsInvoices.Sum(v => v.InvBalance);

    //public List<InvoiceLine> Items => InvoiceItems.ToList();

    public ICommand SaveCommand { get; }

    public InvoiceWithVouchersViewModel(IInvoiceService invoiceService,
                                        ICustomerService customerService,
                                        IInvoiceArReceiptService invoiceArReceiptService)
    {
        //to do  Customers = CustomerService.LoadCustomers();

        _customerService = customerService;
        _invoiceService = invoiceService;
        _invoiceArReceiptService = invoiceArReceiptService;

        _searchOption = new();
        SearchOption.To = Today;
        SearchOption.From = Today.AddDays(-1);
        SearchOption.Filter1 = null;

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

    [RelayCommand]
    private async Task RefreshInvoicesAsync()
    {

        var OsInvlist = await _invoiceService.GetOutStanding(SearchOption);

        if (OsInvlist is not null)
        {
            OsInvoices = new(OsInvlist);

        };

    }

    private async Task PopulateCustomerList()
    {
        //We will implement later - pick all o/s customers list and show here
        //now lets give one customer and fetch the details

        //get customer details - by giving mobile number
       // await CustomerService.GetCustomer

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
            InvReceipts = new(arReceiptsList);

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
}
