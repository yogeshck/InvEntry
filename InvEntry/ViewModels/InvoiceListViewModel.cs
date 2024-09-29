using CommunityToolkit.Mvvm.ComponentModel;
using InvEntry.Models;
using InvEntry.Services;
using InvEntry.Utils;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InvEntry.ViewModels;

public partial class InvoiceListViewModel : ObservableObject
{
    [ObservableProperty]
    private ObservableCollection<InvoiceHeader> _invoices;
    private readonly IInvoiceService _invoiceService;


    public InvoiceListViewModel(IInvoiceService invoiceService) 
    {

        _invoiceService = invoiceService;

        Init();
        //Invoices = new ObservableCollection<InvoiceHeader>();


    }

    private async void Init()
    {
        var invoicesResult = await _invoiceService.GetAll(DateTime.Now.AddDays(-7), DateTime.Now);
        Invoices = new(invoicesResult);
    }

}
