using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DevExpress.Xpf.Editors;
using InvEntry.Extension;
using InvEntry.Services;
using InvEntry.Models;
using InvEntry.Utils;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace InvEntry.ViewModels;

public partial class InvoiceViewModel : ObservableObject
{
    [ObservableProperty]
    private string _customerPhoneNumber;

    [ObservableProperty]
    private Customer _customer;

    [ObservableProperty]
    private InvoiceHeader _header;

    private bool createCustomer = false;
    private readonly ICustomerService _customerService;
    private readonly IProductService _productService;
     
    public InvoiceViewModel(ICustomerService customerService, 
        IProductService productService)
    {
        Header = new()
        {
            InvDate = DateTime.Now,
            InvNbr = InvoiceNumberGenerator.Generate(),
            Lines = new()
        };
        _customerService = customerService;
        _productService = productService;
    }

    [RelayCommand]
    private void FetchCustomer()
    {
        Customer = _customerService.GetCustomer(_customerPhoneNumber);

        if(Customer is null)
        {
            Customer = new();
            createCustomer = true;
        }
    }

    [RelayCommand]
    private void FetchProduct(string productId)
    {
        var product = _productService.GetProduct(productId);

        var invoiceLine = new InvoiceLine()
        {
            ProdNetWeight= Convert.ToDouble(product.NetWeight),
            VaPercent = product.VaPercent,
            ProdQty = 1,
        };

        Header.Lines.Add(invoiceLine);
    }

    [RelayCommand]
    private void CreateInvoice()
    {
        if(Customer is null)
        {
            
        }

        if (createCustomer)
        {

        }
    }
}
