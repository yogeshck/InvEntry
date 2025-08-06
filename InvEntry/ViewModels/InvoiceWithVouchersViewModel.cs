using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Ghostscript.NET.PDFA3Converter.ZUGFeRD;
using InvEntry.Models;
using InvEntry.Services;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace InvEntry.ViewModels;

public partial class InvoiceWithVouchersViewModel : ObservableObject
{
    public ObservableCollection<Customer> Customers { get; set; }
    public ObservableCollection<InvoiceLine> InvoiceItems { get; set; } = new();    
    public ObservableCollection<Voucher> VoucherEntries { get; set; } = new();

    [ObservableProperty] 
    private Customer selectedCustomer;

    [ObservableProperty] 
    private DateTime invoiceDate = DateTime.Now;

    [ObservableProperty] 
    private decimal customerOutstanding;

    public decimal TotalInvoiceAmount => (decimal)InvoiceItems.Sum(i => i.ProdQty * i.InvlBilledPrice);
    public decimal TotalVoucherAmount => (decimal)VoucherEntries.Sum(v => v.PaidAmount);
    public decimal BalanceAmount => TotalInvoiceAmount - TotalVoucherAmount;

    public List<InvoiceLine> Items => InvoiceItems.ToList();

    public ICommand SaveCommand { get; }

    public InvoiceWithVouchersViewModel()
    {
      //to do  Customers = CustomerService.LoadCustomers();
        SaveCommand = new AsyncRelayCommand(SaveAsync);
        PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(SelectedCustomer))
                LoadOutstanding();
            OnPropertyChanged(nameof(TotalInvoiceAmount));
            OnPropertyChanged(nameof(TotalVoucherAmount));
            OnPropertyChanged(nameof(BalanceAmount));
        };
    }

    private async void LoadOutstanding()
    {
     //   if (SelectedCustomer != null)
     //to do       CustomerOutstanding = await CustomerService.GetOutstandingAsync(SelectedCustomer.CustomerId);
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
            GrossRcbAmount = TotalInvoiceAmount,
            Lines = InvoiceItems
        };

        var vouchers = VoucherEntries.Select(v => new Voucher
        {
            VoucherType = v.VoucherType,
            TransAmount = v.TransAmount,
            TransDesc = v.TransDesc,
            VoucherDate = DateTime.Now,
            CustomerGkey = SelectedCustomer.GKey
        }).ToList();

       // await InvoiceService.SaveInvoiceWithVouchersAsync(invoice, vouchers);

        MessageBox.Show("Invoice and vouchers saved.");
        InvoiceItems.Clear();
        VoucherEntries.Clear();
    }
}
