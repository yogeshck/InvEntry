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
using DevExpress.Mvvm;
using DevExpress.Xpf.Grid;

namespace InvEntry.ViewModels;

public partial class InvoiceViewModel : ObservableObject
{
    [ObservableProperty]
    private string _customerPhoneNumber;

    [ObservableProperty]
    private Customer _customer;

    [ObservableProperty]
    private InvoiceHeader _header;

    [ObservableProperty]
    private string _productId;

    private bool createCustomer = false;
    private readonly ICustomerService _customerService;
    private readonly IProductService _productService;
    private readonly IDialogService _dialogService;
    private readonly IInvoiceService _invoiceService;
    private Dictionary<string, Action<object, object>> copyExpression;

    public InvoiceViewModel(ICustomerService customerService, 
        IProductService productService, 
        IDialogService dialogService,
        IInvoiceService invoiceService)
    {
        Header = new()
        {
            InvDate = DateTime.Now,
            InvNbr = InvoiceNumberGenerator.Generate(),
            Lines = new(),
            PaymentMode = "CASH",
            TaxType = "GST"
        };
        _customerService = customerService;
        _productService = productService;
        _dialogService = dialogService;
        _invoiceService = invoiceService;
        PopulateUnboundDataMap();
    }

    private void PopulateUnboundDataMap()
    {
        if (copyExpression is null) copyExpression = new();

        copyExpression.Add($"{nameof(InvoiceLine.InvlTaxableAmount)}Unbound", TaxableUnboundUpdate);
    }

    [RelayCommand]
    private async Task FetchCustomer()
    {
        Customer = await _customerService.GetCustomer(_customerPhoneNumber);

        if(Customer is null)
        {
            Customer = new();
            createCustomer = true;
        }
    }

    [RelayCommand]
    private async Task FetchProduct()
    {
        if (string.IsNullOrEmpty(ProductId)) return;

        var product = await _productService.GetProduct(ProductId);

        InvoiceLine invoiceLine;

        if (product is null)
        {
            invoiceLine = new() { ProdQty = 1 };
        }
        else
        {
            invoiceLine = new InvoiceLine()
            {
                ProdNetWeight = Convert.ToDouble(product.NetWeight),
                VaPercent = product.VaPercent,
                ProdQty = 1,
            };
        }

        Header.Lines.Add(invoiceLine);

        ProductId = string.Empty;
    }

    [RelayCommand]
    private void AddOldJewel(string type)
    {
        var enumVal = Enum.Parse<MetalType>(type);

        var vm = new DialogOldJewelVM();

        if(_dialogService.ShowDialog(MessageButton.OKCancel, "Exchange", "DialogOldJewel", vm) == MessageResult.OK)
        {
            if(enumVal == MetalType.Gold)
            {
                Header.OldGoldAmount = Convert.ToDouble(vm.Rate * vm.Weight);
            }
            else
            {
                Header.OldSilverAmount = Convert.ToDouble(vm.Rate * vm.Weight);
            }
        }
    }

    [RelayCommand]
    private void CreateInvoice()
    {
        if (createCustomer)
            _customerService.CreatCustomer(Customer);

        _invoiceService.CreatHeader(Header);
        _invoiceService.CreatInvoiceLine(Header.Lines);
    }

    [RelayCommand]
    private void CellUpdate(CellValueChangedEventArgs args)
    {
        if (args.Column.FieldName.Contains("Unbound") && copyExpression.TryGetValue(args.Column.FieldName, out var action))
        {
            action.Invoke(args.Row, args.Value);
        }
    }

    private void TaxableUnboundUpdate(object obj, object value)
    {
        if(obj is InvoiceLine item && value is decimal val)
        {
            item.InvlTaxableAmount = Convert.ToDouble(val);
        }
    }
}