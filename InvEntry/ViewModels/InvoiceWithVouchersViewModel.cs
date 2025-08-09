using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Ghostscript.NET.PDFA3Converter.ZUGFeRD;
using InvEntry.Models;
using InvEntry.Services;
using InvEntry.Utils.Options;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
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

    //public decimal TotalInvoiceAmount => (decimal)InvoiceItems.Sum(i => i.ProdQty * i.InvlBilledPrice);
    //public decimal TotalVoucherAmount => (decimal)VoucherEntries.Sum(v => v.PaidAmount);
    //public decimal BalanceAmount => TotalInvoiceAmount - TotalVoucherAmount;

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

     //   PropertyChanged += (_, e) =>
    //    {

     //       SearchOption.To = Today;
     //       SearchOption.From = Today.AddDays(-1);
    //        SearchOption.Filter1 = null; // SelectedCustomer.MobileNbr;

           // Task.Run(RefreshInvoicesAsync).Wait();

            //if (e.PropertyName == nameof(SelectedCustomer))
             //   LoadOutstanding();

            //OnPropertyChanged(nameof(TotalInvoiceAmount));
            //OnPropertyChanged(nameof(TotalVoucherAmount));
            //OnPropertyChanged(nameof(BalanceAmount));
     //   };
    }

    [RelayCommand]
    private async Task RefreshInvoicesAsync()
    { 
        /*        var invoicesResult = await _invoiceService.GetAll(SearchOption);
                if (invoicesResult is not null)
                    Invoices = null;
                Invoices = new(invoicesResult);*/

        await LoadOutstanding();
    }

    private async Task PopulateCustomerList()
    {
        //We will implement later - pick all o/s customers list and show here
        //now lets give one customer and fetch the details

        //get customer details - by giving mobile number
       // await CustomerService.GetCustomer

    }

    private async Task LoadOutstanding()
    {
        //   if (SelectedCustomer != null)
        //to do       CustomerOutstanding = await CustomerService.GetOutstanding(SelectedCustomer.CustomerId);

        var list = await _invoiceService.GetOutStanding(SearchOption);

        if (list is not null)
        {
            OsInvoices = new(list);
        };

    }

    [RelayCommand]
    private async Task SelectionInvoiceChanged()
    {
        if (SelectedInvoice is null) return;

        var arReceiptsList = await _invoiceArReceiptService.GetByInvHdrGKey(SelectedInvoice.GKey);

        if (arReceiptsList is not null)
            InvReceipts = new(arReceiptsList);

        //var grnLineListResult = await _grnService.GetByHdrGkey(SelectedGrn.GKey);
        //if (grnLineListResult is not null)
        //    GrnLineList = new(grnLineListResult);
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
