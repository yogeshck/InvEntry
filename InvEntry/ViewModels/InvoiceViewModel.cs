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
using DevExpress.Xpf.Core.Native;

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
    public bool _customerReadOnly;

    private bool createCustomer = false;
    private readonly ICustomerService _customerService;
    private readonly IProductService _productService;
    private readonly IDialogService _dialogService;
    private readonly IMessageBoxService _messageBoxService;
    private readonly IInvoiceService _invoiceService;
    private Dictionary<string, Action<InvoiceLine, decimal?>> copyInvoiceExpression;
    private Dictionary<string, Action<InvoiceHeader, decimal?>> copyHeaderExpression;
    private decimal IGSTPercent = 0M;
    private decimal SCGSTPercent = 3M;

    private List<string> IGNORE_UPDATE = new List<string>
    {
        nameof(InvoiceLine.VaAmount)
    };

    public InvoiceViewModel(ICustomerService customerService,
        IProductService productService,
        IDialogService dialogService,
        IInvoiceService invoiceService,
        IMessageBoxService messageBoxService)
    {
        SetHeader();
        _customerService = customerService;
        _productService = productService;
        _dialogService = dialogService;
        _invoiceService = invoiceService;
        _messageBoxService = messageBoxService;
        _currentRate = 2600;
        _customerReadOnly = true;
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

        copyHeaderExpression.Add($"{nameof(InvoiceHeader.RoundOff)}", (item, val) => item.RoundOff = val);
        copyHeaderExpression.Add($"{nameof(InvoiceHeader.GrossRcbAmount)}", (item, val) => item.GrossRcbAmount = val);
        copyHeaderExpression.Add($"{nameof(InvoiceHeader.AmountPayable)}", (item, val) => item.AmountPayable = val);
        copyHeaderExpression.Add($"{nameof(InvoiceHeader.InvBalance)}", (item, val) => item.InvBalance = val);
    }

    [RelayCommand]
    private async Task FetchCustomer(EditValueChangedEventArgs args)
    {
        if (args.NewValue is not string phoneNumber) return;

        if (string.IsNullOrEmpty(phoneNumber) || phoneNumber.Length < 10)
            return;

        if (Customer is not null && Customer.MobileNbr == phoneNumber)
            return;

        Messenger.Default.Send(MessageType.WaitIndicator, WaitIndicatorVM.ShowIndicator("Fetching Customer details..."));

        Customer = await _customerService.GetCustomer(phoneNumber);

        Messenger.Default.Send(MessageType.WaitIndicator, WaitIndicatorVM.HideIndicator());

        if (Customer is null)
        {
            _messageBoxService.ShowMessage("No customer details found.", "Customer not found", MessageButton.OK);
            Customer = new();
            createCustomer = true;
            CustomerReadOnly = false;
            Messenger.Default.Send("CustomerNameUI", MessageType.FocusTextEdit);
        }
        else
        {
            CustomerReadOnly = true;
            Messenger.Default.Send("ProductIdUIName", MessageType.FocusTextEdit);
            IGSTPercent = Customer.GstStateCode == "33" ? 0M : 3M;
        }

        Header.InvCustMobile = phoneNumber;
    }

    [RelayCommand]
    private async Task FetchProduct()
    {
        if (string.IsNullOrEmpty(ProductIdUI)) return;

        var product = await _productService.GetProduct(ProductIdUI);

        if (product is null)
        {
            _messageBoxService.ShowMessage($"No Product found for {ProductIdUI}, Please make sure it exists",
                "Product not found", MessageButton.OK, MessageIcon.Error);
            return;
        }

        InvoiceLine invoiceLine = new InvoiceLine()
        {
            ProdQty = 1,
            InvlBilledPrice = CurrentRate,
            InvlCgstPercent = GetGSTWithinState(),
            InvlSgstPercent = GetGSTWithinState(),
            InvlIgstPercent = IGSTPercent,
            InvlStoneAmount = 0M,
            InvoiceId = Header.InvNbr
        };

        invoiceLine.SetProductDetails(product);

        EvaluateFormula(invoiceLine, isInit: true);

        Header.Lines.Add(invoiceLine);

        ProductIdUI = string.Empty;

        EvaluateHeader(null);
    }

    [RelayCommand]
    private void AddOldJewel(string type)
    {
        var enumVal = Enum.Parse<MetalType>(type);

        var vm = new DialogOldJewelVM();

        if (_dialogService.ShowDialog(MessageButton.OKCancel, "Exchange", "DialogOldJewel", vm) == MessageResult.OK)
        {
            if (enumVal == MetalType.Gold)
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
        if (Customer is null || string.IsNullOrEmpty(Customer.CustomerName))
        {
            _messageBoxService.ShowMessage("Customer information is not provided", "Customer info", MessageButton.OK, MessageIcon.Hand);
            return;
        }

        if (createCustomer)
            _customerService.CreatCustomer(Customer);

        if (Header.InvBalance > 0)
        {
            Header.PymtDueDate = Header.InvDate.Value.AddDays(7);
        }

        _invoiceService.CreatHeader(Header);
        _invoiceService.CreatInvoiceLine(Header.Lines);

        SetHeader();
    }

    [RelayCommand]
    private void CellUpdate(CellValueChangedEventArgs args)
    {
        if (args.Row is InvoiceLine line)
        {
            EvaluateFormula(line);
        }

        EvaluateHeader(null);
    }

    [RelayCommand]
    private void EvaluateHeader(EditValueChangedEventArgs args)
    {
        Header.InvlTaxTotal = Header.Lines.Select(x => x.InvlTotal).Sum();

        Header.RoundOff = MathUtils.Normalize(Math.Round(Header.InvlTaxTotal.GetValueOrDefault(), 0) - Header.InvlTaxTotal.GetValueOrDefault());

        Header.GrossRcbAmount = MathUtils.Normalize(Header.InvlTaxTotal.GetValueOrDefault() + Header.RoundOff.GetValueOrDefault() - Header.DiscountAmount.GetValueOrDefault());

        Header.AmountPayable = MathUtils.Normalize(Header.GrossRcbAmount.GetValueOrDefault() - Header.OldGoldAmount.GetValueOrDefault() - Header.OldSilverAmount.GetValueOrDefault());

        Header.InvBalance = MathUtils.Normalize(Header.AmountPayable.GetValueOrDefault() - Header.AdvanceAdj.GetValueOrDefault() - Header.RdAmountAdj.GetValueOrDefault() - Header.RecdAmount.GetValueOrDefault());
    }

    [RelayCommand]
    private void Focus(TextEdit sender)
    {
        sender.Focus();
    }

    private void SetHeader()
    {
        Header = new()
        {
            InvDate = DateTime.Now,
            InvNbr = InvoiceNumberGenerator.Generate()
        };
    }

    private decimal GetGSTWithinState()
    {
        if (Customer.GstStateCode == "33")
        {
            return Math.Round(SCGSTPercent / 2, 3);
        }
        return 0M;
    }

    private void EvaluateForAllLines()
    {
        foreach (var line in Header.Lines)
        {
            EvaluateFormula(line);
        }
    }

    private void EvaluateFormula<T>(T item, bool isInit = false) where T : class
    {
        var formulas = FormulaStore.Instance.GetFormulas<T>();

        foreach (var formula in formulas)
        {
            if (!isInit && IGNORE_UPDATE.Contains(formula.FieldName)) continue;

            var val = formula.Evaluate<T, decimal>(item, 0M);

            if (item is InvoiceLine invLine)
                copyInvoiceExpression[formula.FieldName].Invoke(invLine, val);
        }
    }

    private void EvaluateFormula<T>(T item, string fieldName, bool isInit = false) where T : class
    {
        if (!isInit && IGNORE_UPDATE.Contains(fieldName)) return;

        var formula = FormulaStore.Instance.GetFormula<T>(fieldName);

        var val = formula.Evaluate<T, decimal>(item, 0M);

        if (item is InvoiceLine invLine)
            copyInvoiceExpression[fieldName].Invoke(invLine, val);
        else if (item is InvoiceHeader head)
            copyHeaderExpression[fieldName].Invoke(head, val);
    }
}