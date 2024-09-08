using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DevExpress.Xpf.Editors;
using InvEntry.Extension;
using InvEntry.Services;
using InvEntry.UIModels;
using InvEntry.Utils;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tern.MI.InvEntry.Models;

namespace InvEntry.ViewModels;

public partial class InvoiceViewModel : ObservableObject
{
    [ObservableProperty]
    private string _customerPhoneNumber;

    [ObservableProperty]
    private Customer _customer;

    [ObservableProperty]
    private InvoiceHeader _header;

    private readonly ICustomerService _customerService;
    private readonly IProductService _productService;
     
    public InvoiceViewModel(ICustomerService customerService, IProductService productService)
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
    }

    [RelayCommand]
    private void FetchProduct(decimal productKey)
    {
        var key = Decimal.ToInt64(productKey);

        var product = _productService.GetProduct(key);

        if (Header.Lines.Any(x => x.ProductGkey == key))
        {
            var line = Header.Lines.First(x => x.ProductGkey == key);
            line.ProdQty += 1;
            line.InvlBilledPrice = line.ProdQty * product.GrossAmount;
            return;
        }

        var invoiceLine = new InvoiceLine()
        {
            ProductGkey = product.ProductGkey,
            ProductName = product.ProductName,
            InvlGrossAmt = product.GrossAmount,
            InvlBilledPrice = product.GrossAmount,
            ProdQty = 1,
        };

        Header.Lines.Add(invoiceLine);
    }


}
