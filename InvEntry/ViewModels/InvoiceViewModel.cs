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
using DevExpress.Mvvm.Native;

namespace InvEntry.ViewModels;

public partial class InvoiceViewModel : ObservableObject
{
    [ObservableProperty]
    private string _customerPhoneNumber;

    [ObservableProperty]
    private Customer _buyer;

    [ObservableProperty]
    private InvoiceHeader _header;

    [ObservableProperty]
    private string _productIdUI;

    [ObservableProperty]
    private decimal _currentRate;

    [ObservableProperty]
    public bool _customerReadOnly;

    [ObservableProperty]
    public bool _isPrint;

    [ObservableProperty]
    private ObservableCollection<InvoiceLine> selectedRows;

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
        _isPrint = false;
        PopulateUnboundLineDataMap();
        PopulateUnboundHeaderDataMap();
    }

    private void PopulateUnboundLineDataMap()
    {
        if (copyInvoiceExpression is null) copyInvoiceExpression = new();

        copyInvoiceExpression.Add($"{nameof(InvoiceLine.InvlTaxableAmount)}", (item, val) => item.InvlTaxableAmount = val);
        copyInvoiceExpression.Add($"{nameof(InvoiceLine.ProdNetWeight)}", (item, val) => item.ProdNetWeight = val);
        copyInvoiceExpression.Add($"{nameof(InvoiceLine.InvlGrossAmt)}", (item, val) => item.InvlGrossAmt = val);
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

        if (Buyer is not null && Buyer.MobileNbr == phoneNumber)
            return;

        Messenger.Default.Send(MessageType.WaitIndicator, WaitIndicatorVM.ShowIndicator("Fetching Customer details..."));

        Buyer = await _customerService.GetCustomer(phoneNumber);

        Messenger.Default.Send(MessageType.WaitIndicator, WaitIndicatorVM.HideIndicator());

        if (Buyer is null)
        {
            _messageBoxService.ShowMessage("No customer details found.", "Customer not found", MessageButton.OK);
            Buyer = new();
            createCustomer = true;
            CustomerReadOnly = false;
            Messenger.Default.Send("CustomerNameUI", MessageType.FocusTextEdit);
        }
        else
        {
            CustomerReadOnly = true;
            Header.GstLocBuyer = Buyer.GstStateCode;
            Messenger.Default.Send("ProductIdUIName", MessageType.FocusTextEdit);
            IGSTPercent = Buyer.GstStateCode == "33" ? 0M : 3M;
        }

        Header.CustMobile = phoneNumber;
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
            TaxType = "GST"
        };

        invoiceLine.SetProductDetails(product);

        EvaluateFormula(invoiceLine, isInit: true);

        Header.Lines.Add(invoiceLine);

        ProductIdUI = string.Empty;

        EvaluateHeader();
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
        if (Buyer is null || string.IsNullOrEmpty(Buyer.CustomerName))
        {
            _messageBoxService.ShowMessage("Customer information is not provided", "Customer info", MessageButton.OK, MessageIcon.Hand);
            return;
        }

        if (createCustomer)
            _customerService.CreatCustomer(Buyer);

        Header.InvNbr = InvoiceNumberGenerator.Generate();
        Header.CustGkey = Buyer.GKey;
        Header.CgstAmount = Header.Lines.Select(x => x.InvlCgstAmount).Sum();
        Header.SgstAmount = Header.Lines.Select(x => x.InvlSgstAmount).Sum();
        Header.IgstAmount = Header.Lines.Select(x => x.InvlIgstAmount).Sum();

        Header.CgstPercent = Header.Lines.FirstOrDefault().InvlCgstPercent;
        Header.SgstPercent = Header.Lines.FirstOrDefault().InvlSgstPercent;
        Header.IgstPercent = Header.Lines.FirstOrDefault().InvlIgstPercent;

        Header.Lines.ForEach(x =>
        {
            x.InvLineNbr = Header.Lines.IndexOf(x) + 1;
            x.InvoiceId = Header.InvNbr;
        });
        _invoiceService.CreatHeader(Header);
        _invoiceService.CreatInvoiceLine(Header.Lines);

        Buyer = null;

        _messageBoxService.ShowMessage("Invoice Created Successfully", "Invoice Created", MessageButton.OK, MessageIcon.None);
        IsPrint = true;
    }

    [RelayCommand]
    private void PrintInvoice()
    {
        CustomerPhoneNumber = null;
        _messageBoxService.ShowMessage("Invoice printed Successfully", "Invoice print", MessageButton.OK, MessageIcon.None);
        SetHeader();
        IsPrint = false;
    }

    [RelayCommand]
    private void CellUpdate(CellValueChangedEventArgs args)
    {
        if (args.Row is InvoiceLine line)
        {
            EvaluateFormula(line);
        }

        EvaluateHeader();
    }

    [RelayCommand]
    private void EvaluateHeader()
    {
        Header.InvlTaxTotal = Header.Lines.Select(x => x.InvlTotal).Sum();

        Header.RoundOff = MathUtils.Normalize(Math.Round(Header.InvlTaxTotal.GetValueOrDefault(), 0) - Header.InvlTaxTotal.GetValueOrDefault());

        Header.GrossRcbAmount = MathUtils.Normalize(Header.InvlTaxTotal.GetValueOrDefault() + Header.RoundOff.GetValueOrDefault() - Header.DiscountAmount.GetValueOrDefault());

        Header.AmountPayable = MathUtils.Normalize(Header.GrossRcbAmount.GetValueOrDefault() - Header.OldGoldAmount.GetValueOrDefault() - Header.OldSilverAmount.GetValueOrDefault());

        Header.InvBalance = MathUtils.Normalize(Header.AmountPayable.GetValueOrDefault() - Header.AdvanceAdj.GetValueOrDefault() - Header.RdAmountAdj.GetValueOrDefault() - Header.RecdAmount.GetValueOrDefault());

        if (Header.InvBalance > 0 && !Header.PaymentDueDate.HasValue)
        {
            Header.PaymentDueDate = Header.InvDate.Value.AddDays(7);
        }
    }

    [RelayCommand]
    private void Focus(TextEdit sender)
    {
        sender.Focus();
    }

    [RelayCommand]
    private void DeleteRows()
    {
        List<int> indexs = new List<int>();
       foreach(var row in SelectedRows)
       {
            indexs.Add(Header.Lines.IndexOf(row));
       }

        indexs.ForEach(x => 
        {
            if(x >= 0)
            {
                Header.Lines.RemoveAt(x);
            }
        });

        EvaluateForAllLines();
        EvaluateHeader();
    }

    private void SetHeader()
    {
        Header = new()
        {
            InvDate = DateTime.Now,
            IsTaxApplicable = true,
            GstLocSeller = "33"
        };
    }

    private decimal GetGSTWithinState()
    {
        if (Buyer.GstStateCode == "33")
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