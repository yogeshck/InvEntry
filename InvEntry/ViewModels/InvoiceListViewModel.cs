using CommunityToolkit.Mvvm.ComponentModel;
using InvEntry.Models;
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

    public InvoiceListViewModel() 
    {
        Invoices = new ObservableCollection<InvoiceHeader>();

        Invoices.Add(new InvoiceHeader()
        {
            InvNbr = InvoiceNumberGenerator.Generate(),
            InvDate = DateTime.Now.AddDays(-1)
        });


        Invoices.Add(new InvoiceHeader()
        {
            InvNbr = InvoiceNumberGenerator.Generate(),
            InvDate = DateTime.Now.AddDays(-1)
        });


        Invoices.Add(new InvoiceHeader()
        {
            InvNbr = InvoiceNumberGenerator.Generate(),
            InvDate = DateTime.Now.AddDays(-1)
        });
    }
}
