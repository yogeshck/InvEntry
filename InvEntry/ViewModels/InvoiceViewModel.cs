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
using InvEntry.Store;
using DevExpress.XtraSpreadsheet.Model;
using System.Windows.Input;

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

    [ObservableProperty]
    private bool _lineView;

    [ObservableProperty]
    public bool _headerView;

    private bool createCustomer = false;
    private readonly ICustomerService _customerService;
    private readonly IProductService _productService;
    private readonly IDialogService _dialogService;
    private readonly IInvoiceService _invoiceService;
    private Dictionary<string, Action<InvoiceLine, decimal?>> copyInvoiceExpression;
    private Dictionary<string, Action<InvoiceHeader, decimal?>> copyHeaderExpression;

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
        LineView = true;
        HeaderView = false;
        PopulateUnboundLineDataMap();
        PopulateUnboundHeaderDataMap();
    }

    private void PopulateUnboundLineDataMap()
    {
        if (copyInvoiceExpression is null) copyInvoiceExpression = new();

        copyInvoiceExpression.Add($"{nameof(InvoiceLine.InvlTaxableAmount)}", (item, val) => item.InvlTaxableAmount = val);
        copyInvoiceExpression.Add($"{nameof(InvoiceLine.ProdNetWeight)}", (item, val) => item.ProdNetWeight = val);
        copyInvoiceExpression.Add($"{nameof(InvoiceLine.VaAmount)}", (item, val) => item.VaAmount = val);
        copyInvoiceExpression.Add($"{nameof(InvoiceLine.InvlCgstAmount)}", (item, val) => item.InvlCgstAmount = val);
        copyInvoiceExpression.Add($"{nameof(InvoiceLine.InvlSgstAmount)}", (item, val) => item.InvlSgstAmount = val);
        copyInvoiceExpression.Add($"{nameof(InvoiceLine.InvlIgstAmount)}", (item, val) => item.InvlIgstAmount = val);
        copyInvoiceExpression.Add($"{nameof(InvoiceLine.InvlTotal)}", (item, val) => item.InvlTotal = val);
    }

    private void PopulateUnboundHeaderDataMap()
    {
        if (copyHeaderExpression is null) copyHeaderExpression = new();

        copyHeaderExpression.Add($"{nameof(InvoiceHeader.GrossRcbAmount)}", (item, val) => item.GrossRcbAmount = val);
        copyHeaderExpression.Add($"{nameof(InvoiceHeader.AmountPayable)}", (item, val) => item.AmountPayable = val);
        copyHeaderExpression.Add($"{nameof(InvoiceHeader.InvBalance)}", (item, val) => item.InvBalance = val);
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

        if(product is not null)
            EvaluateFormula(invoiceLine);

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
        if (args.Row is InvoiceLine line)
        {
            EvaluateFormula(line);
        }

        Header.InvlTaxTotal = Header.Lines.Select(x => x.InvlTotal).Sum();
        EvaluateFormula(Header);
    }

    [RelayCommand]
    private void ToggleVisibility()
    {
        LineView ^= true;
        HeaderView = !LineView;
    }

    private void SetHeader()
    {
        Header = new()
        {
            InvDate = DateTime.Now,
            InvNbr = InvoiceNumberGenerator.Generate(),
            Lines = new(),
            PaymentMode = "CASH",
            TaxType = "GST",
            TenantGkey = "1",
        };
    }

    private decimal GetGSTWithinState()
    {
        return Convert.ToDecimal(0.03/2);
    }

    private void EvaluateFormula<T>(T line) where T: class
    {
        var formulas = FormulaStore.Instance.GetFormulas<T>();

        foreach(var formula in formulas)
        {
            var val = formula.Evaluate<T, decimal>(line);

            if(line is InvoiceLine invLine)
                copyInvoiceExpression[formula.FieldName].Invoke(invLine, val);
            
        }
    }

    private void EvaluateFormula<T>(T line, string fieldName) where T : class
    {
        var formula = FormulaStore.Instance.GetFormula<T>(fieldName);

        var val = formula.Evaluate<T, decimal>(line);

        if (line is InvoiceLine invLine)
            copyInvoiceExpression[fieldName].Invoke(invLine, val);
        else if(line is InvoiceHeader head)
            copyHeaderExpression[fieldName].Invoke(head, val);
    }


}