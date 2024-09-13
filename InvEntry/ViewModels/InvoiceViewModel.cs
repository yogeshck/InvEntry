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
using InvEntry.Models.Extensions;

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
    private string _productIdUI;

    [ObservableProperty]
    private decimal _currentRate;

    private bool createCustomer = false;
    private readonly ICustomerService _customerService;
    private readonly IProductService _productService;
    private readonly IDialogService _dialogService;
    private readonly IInvoiceService _invoiceService;
    private Dictionary<string, Action<InvoiceLine, decimal>> copyExpression;

    public InvoiceViewModel(ICustomerService customerService, 
        IProductService productService, 
        IDialogService dialogService,
        IInvoiceService invoiceService)
    {
        SetHeader();
        _customerService = customerService;
        _productService = productService;
        _dialogService = dialogService;
        _invoiceService = invoiceService;
        _currentRate = 2600;
        PopulateUnboundDataMap();
    }

    private void PopulateUnboundDataMap()
    {
        if (copyExpression is null) copyExpression = new();

        copyExpression.Add($"{nameof(InvoiceLine.InvlTaxableAmount)}Unbound", (item, val) => item.InvlTaxableAmount = val);
        copyExpression.Add($"{nameof(InvoiceLine.ProdNetWeight)}Unbound", (item, val) => item.ProdNetWeight = val);
        copyExpression.Add($"{nameof(InvoiceLine.VaAmount)}Unbound", (item, val) => item.VaAmount = val);
        copyExpression.Add($"{nameof(InvoiceLine.InvlCgstAmount)}Unbound", (item, val) => item.InvlCgstAmount = val);
        copyExpression.Add($"{nameof(InvoiceLine.InvlSgstAmount)}Unbound", (item, val) => item.InvlSgstAmount = val);
        copyExpression.Add($"{nameof(InvoiceLine.InvlIgstAmount)}Unbound", (item, val) => item.InvlIgstAmount = val);
        copyExpression.Add($"{nameof(InvoiceLine.InvlTotal)}Unbound", (item, val) => item.InvlTotal = val);
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
        if (string.IsNullOrEmpty(ProductIdUI)) return;

        var product = await _productService.GetProduct(ProductIdUI);

        InvoiceLine invoiceLine = new InvoiceLine()
        {
            ProdQty = 1,
            InvlBilledPrice = CurrentRate,
            InvlCgstPercent = GetGSTWithinState(),
            InvlSgstPercent = GetGSTWithinState(),
            InvlIgstPercent = 0.03M
        };

        if (product is not null)
        {
            invoiceLine.SetProductDetails(product);
        }

        Header.Lines.Add(invoiceLine);

        ProductIdUI = string.Empty;
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
                Header.OldGoldAmount = vm.Rate * vm.Weight;
            }
            else
            {
                Header.OldSilverAmount = vm.Rate * vm.Weight;
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
        if (args.Column.FieldName.Contains("Unbound") && args.Row is InvoiceLine line && args.Value is decimal value
            && copyExpression.TryGetValue(args.Column.FieldName, out var action))
        {
            action.Invoke(line, value);
        }
    }

    private void SetHeader()
    {
        Header = new()
        {
            InvDate = DateTime.Now,
            InvNbr = InvoiceNumberGenerator.Generate(),
            Lines = new(),
            PaymentMode = "CASH",
            TaxType = "GST"
        };
    }

    private decimal GetGSTWithinState()
    {
        return Convert.ToDecimal(0.03/2);
    }
}