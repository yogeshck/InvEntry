using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DevExpress.CodeParser;
using DevExpress.Mvvm;
using DevExpress.Mvvm.Native;
using DevExpress.Xpf.Editors;
using DevExpress.Xpf.Grid;
using InvEntry.Extension;
using InvEntry.Helpers;
using InvEntry.Mappers.CustomerOrders;
using InvEntry.Models;
using InvEntry.Models.Extensions;
using InvEntry.Reports;
using InvEntry.Services;
using InvEntry.Services.Customers;
using InvEntry.Store;
using InvEntry.Utils.Options;
using InvEntry.ViewModels.Common;
using InvEntry.Views.Common;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using IDialogService = DevExpress.Mvvm.IDialogService;

namespace InvEntry.ViewModels;

public partial class CustomerOrderViewModel : ObservableObject
{
    [ObservableProperty]
    private string _customerPhoneNumber;

    [ObservableProperty]
    private string _customerState;
    //private MtblReference _customerState;

    //[ObservableProperty]
    //private MtblReference _salesPerson;

    [ObservableProperty]
    private Customer _buyer;

    [ObservableProperty]
    private OrgThisCompanyView _company;

    [ObservableProperty]
    private CustomerOrder _header;

    [ObservableProperty]
    private LedgersHeader _ledgerHeader;

    [ObservableProperty]
    private string _productIdUI;

    [ObservableProperty]
    private string _orderStatusUI;

    [ObservableProperty]
    private MtblLedger _mtblLedger;

    [ObservableProperty]
    private string _productSku;

    [ObservableProperty]
    private string _oldMetalIdUI;

    [ObservableProperty]
    public bool _customerReadOnly;

    [ObservableProperty]
    public bool _isRefund;

    [ObservableProperty]
    public bool _isBalance;

    [ObservableProperty]
    private bool isOrderSearchVisible;

    [ObservableProperty]
    private bool hasValidationErrors;

    [ObservableProperty]
    private ObservableCollection<string> validationErrors = new();

    [ObservableProperty]
    private ObservableCollection<CustomerOrderLine> selectedRows;

    /*    [ObservableProperty]
        private ObservableCollection<ProductView> productStockList;*/

    [ObservableProperty]
    private ObservableCollection<string> productCategoryList;

    [ObservableProperty]
    private ObservableCollection<string> metalList;

    [ObservableProperty]
    private ObservableCollection<string> _custOrdStatusList;
    // private ObservableCollection<MtblReference> custOrdStatusList; 

    [ObservableProperty]
    private ObservableCollection<string> _paymentModeList;

    private ProductView OldMetalProductView;

    //[ObservableProperty]
    //private ObservableCollection<MtblReference> mtblReferencesList;

    //[ObservableProperty]
    //private ObservableCollection<string> _salesPersonReferencesList;
    //private ObservableCollection<MtblReference> salesPersonReferencesList;

    [ObservableProperty]
    private ObservableCollection<string> _stateReferencesList;
    //private ObservableCollection<MtblReference> stateReferencesList;

    [ObservableProperty]
    private string _searchText;

    [ObservableProperty]
    private DateSearchOption _searchOption;

    private bool createCustomer = false;
    private bool updateOrder = false;
    private bool invBalanceChk = false;
    private decimal todaysRate;

    private readonly ReferenceLoader _referenceLoader;
    private readonly ICustomerService _customerService;
    private readonly IProductViewService _productViewService;
    private readonly IProductStockService _productStockService;
    private readonly IProductStockSummaryService _productStockSummaryService;
    private readonly IProductTransactionService _productTransactionService;
    //private readonly IProductTransactionSummaryService _productTransactionSummaryService;
    private readonly IDialogService _dialogService;
    private readonly IDialogService _reportDialogService;
    private readonly IMessageBoxService _messageBoxService;
    private readonly IVoucherService _voucherService;
    private readonly ICustomerOrderService _customerOrderService;
    private readonly ILedgerService _ledgerService;
    private readonly IProductCategoryService _productCategoryService;
    private readonly IInvoiceArReceiptService _invoiceArReceiptService;
    private readonly IOrgThisCompanyViewService _orgThisCompanyViewService;
    private readonly IOldMetalTransactionService _oldMetalTransactionService;
    private readonly IMtblReferencesService _mtblReferencesService;
    private readonly IMtblLedgersService _mtblLedgersService;
    private readonly IReportFactoryService _reportFactoryService;
    private readonly ICustomerLookupService _customerLookupService;

    private SettingsPageViewModel _settingsPageViewModel;
    private Dictionary<string, Action<CustomerOrderLine, decimal?>> copyCustomerOrderLineExpression;
    private Dictionary<string, Action<CustomerOrder, decimal?>> copyCustomerOrderExpression;

    public AsyncCommand LoadReferencesCommand { get; }
    //private Dictionary<int, string> orderStatus = new Dictionary<int, string>();
    //private Dictionary<string, MtblReference> dictionaryOrderStatus = new Dictionary<string, MtblReference>();


    public CustomerOrderViewModel(

            ICustomerService customerService,
            ICustomerLookupService customerLookupService,
            IProductViewService productViewService,
            IProductStockService productStockService,
            IProductStockSummaryService productStockSummaryService,
            IProductTransactionService productTransactionService,
            //IProductTransactionSummaryService productTransactionSummaryService,
            IDialogService dialogService,
            ICustomerOrderService custOrderService,
            ILedgerService ledgerService,
            IProductCategoryService productCategoryService,
            IMessageBoxService messageBoxService,
            IVoucherService voucherService,
            IInvoiceArReceiptService invoiceArReceiptService,
            IOrgThisCompanyViewService orgThisCompanyViewService,
            IOldMetalTransactionService oldMetalTransactionService,
            IMtblReferencesService mtblReferencesService,
            IMtblLedgersService mtblLedgersService,
            SettingsPageViewModel settingsPageViewModel,
            IReportFactoryService reportFactoryService,
            ReferenceLoader referenceLoader,
            [FromKeyedServices("ReportDialogService")] IDialogService reportDialogService)
    {
        // Assign dependencies
        _orgThisCompanyViewService = orgThisCompanyViewService;
        _customerService = customerService;
        _customerLookupService = customerLookupService;
        _productViewService = productViewService;
        _productStockService = productStockService;
        _productStockSummaryService = productStockSummaryService;
        _productTransactionService = productTransactionService;
        _productCategoryService = productCategoryService;
        _dialogService = dialogService;
        _customerOrderService = custOrderService;
        _ledgerService = ledgerService;
        _messageBoxService = messageBoxService;
        _mtblLedgersService = mtblLedgersService;
        _reportDialogService = reportDialogService;
        _reportFactoryService = reportFactoryService;
        _oldMetalTransactionService = oldMetalTransactionService;
        _voucherService = voucherService;
        _invoiceArReceiptService = invoiceArReceiptService;
        _mtblReferencesService = mtblReferencesService;
        _settingsPageViewModel = settingsPageViewModel;

        _referenceLoader = referenceLoader;

        selectedRows = new();
        _customerReadOnly = false;
        _isBalance = true;
        _isRefund = false;

        // Start async init
        _ = InitializeAsync();

        SetHeader();

        //PopulateUnboundHeaderDataMap();
    }

    private async Task InitializeAsync()
    {
        try
        {
            await SetThisCompany();
            SetHeader();

            SetMetalPrice();
            await SetMasterLedger();

            _ = LoadReferencesAsync();

            await PopulateProductCategoryList();
            //await PopulateStateList();
            //await PopulateMtblRefNameList();
            await PopulateMetalList();
            //await PopulateOrderStatusList();
            //await PopulateSalesPersonList();
            PopulateUnboundLineDataMap();
        }
        catch (Exception ex)
        {
            _messageBoxService.ShowMessage("Initialization failed: " + ex.Message, "Startup Error", MessageButton.OK, MessageIcon.Error);
        }
    }

    private async Task SetThisCompany()
    {
        Company = new();
        Company = await _orgThisCompanyViewService.GetOrgThisCompany();
    }

    private void SetMetalPrice()
    {
        var metalPrice = getBilledPrice("GOLD");
        if (metalPrice < 1)
        {
            displayRateErrorMsg();
            //return;
        }

        todaysRate = (decimal)metalPrice;
    }

    private void displayRateErrorMsg()
    {
        _messageBoxService.ShowMessage($"Todays Rate not entered in system, set the rate and start invoicing....",
                                        "Todays Rate not found", MessageButton.OK, MessageIcon.Error);

    }

    private decimal getBilledPrice(string metal)
    {
        var metalPrice = _settingsPageViewModel.GetPrice(metal);

        if (metalPrice is null)
        {
            metalPrice = -1;
        }

        return (decimal)metalPrice;
    }

    private async Task SetMasterLedger()
    {
        MtblLedger = await _mtblLedgersService.GetLedger(1000);
    }

    private void SetHeader()
    {
        Header = new()
        {
            OrderDate = DateTime.Now,
            OrderType = "New",
            OrderStatusFlag = 1,    // 1 - Open,  2 - In-Progress,   3 - Completed,   4 - Delivered
            OrderDueDate = DateTime.Now.AddDays(14),   //hard coded should be from references....
            //IsTaxApplicable = true,
            //     GstLocSeller = Company.GstCode,
            TenantGkey = Company.TenantGkey
        };

        // OrderStatus = CustOrdStatusList.FirstOrDefault(x => x..Equals("1")).ToString();
        OrderStatusUI = "OPEN";
    }

    private async Task LoadReferencesAsync()
    {

        CustOrdStatusList = await _referenceLoader.LoadValuesAsync("CUST_ORD_STATUS");

        StateReferencesList = await _referenceLoader.LoadValuesAsync("CUST_STATE");

        PaymentModeList = await _referenceLoader.LoadValuesAsync("PAYMENT_MODE");

        //SalesPersonReferencesList = await _referenceLoader.LoadValuesAsync("SALES_PERSON");

    }

    private async Task PopulateProductCategoryList()
    {
        var list = await _productCategoryService.GetProductCategoryList();
        ProductCategoryList = new(list.Select(x => x.Name));
    }

    private void PopulateUnboundLineDataMap()
    {
        if (copyCustomerOrderLineExpression is null) copyCustomerOrderLineExpression = new();

        //    copyCustomerOrderLineExpression.Add($"{nameof(CustomerOrderLine.InvlTaxableAmount)}", (item, val) => item.InvlTaxableAmount = val);
        copyCustomerOrderLineExpression.Add($"{nameof(CustomerOrderLine.ProdNetWeight)}", (item, val) => item.ProdNetWeight = val);
        //    copyCustomerOrderLineExpression.Add($"{nameof(CustomerOrderLine.InvlGrossAmt)}", (item, val) => item.InvlGrossAmt = val * (item.Metal.Equals("DIAMOND") ? 100 : 1));
        copyCustomerOrderLineExpression.Add($"{nameof(CustomerOrderLine.VaAmount)}", (item, val) => item.VaAmount = val);
        //    copyCustomerOrderLineExpression.Add($"{nameof(CustomerOrderLine.InvlCgstAmount)}", (item, val) => item.InvlCgstAmount = val);
        //    copyCustomerOrderLineExpression.Add($"{nameof(CustomerOrderLine.InvlSgstAmount)}", (item, val) => item.InvlSgstAmount = val);
        //    copyCustomerOrderLineExpression.Add($"{nameof(CustomerOrderLine.InvlIgstAmount)}", (item, val) => item.InvlIgstAmount = val);
        //    copyCustomerOrderLineExpression.Add($"{nameof(CustomerOrderLine.InvlTotal)}", (item, val) => item.InvlTotal = val);
    }

    private void PopulateUnboundHeaderDataMap()
    {
        if (copyCustomerOrderExpression is null) copyCustomerOrderExpression = new();

        //    copyCustomerOrderExpression.Add($"{nameof(CustomerOrder.RoundOff)}", (item, val) => item.RoundOff = val);
        //    copyCustomerOrderExpression.Add($"{nameof(CustomerOrder.GrossRcbAmount)}", (item, val) => item.GrossRcbAmount = val);
        //    copyCustomerOrderExpression.Add($"{nameof(CustomerOrder.AmountPayable)}", (item, val) => item.AmountPayable = val);
        //    copyCustomerOrderExpression.Add($"{nameof(CustomerOrder.InvBalance)}", (item, val) => item.InvBalance = val);
    }

    private async Task PopulateMetalList()
    {
        var metalRefList = await _mtblReferencesService.GetReferenceList("OLD_METALS");
        MetalList = new(metalRefList.Select(x => x.RefValue));
    }

    [RelayCommand]
    private void ToggleOrderSearch()
    {
        IsOrderSearchVisible = !IsOrderSearchVisible;

        if (!IsOrderSearchVisible)
        {
            SearchText = string.Empty;
        }
    }


        private async Task PopulateOrderLines()
    {
        var lines =
            (await _customerOrderService
                .GetLines(Header.OrderNbr))
            .ToList();

        System.Diagnostics.Debug.WriteLine(
            $"Order {Header.OrderNbr}: {lines.Count} lines returned");

        foreach (var line in lines)
        {
            System.Diagnostics.Debug.WriteLine(
                $"Line={line.OrderLineNbr}, " +
                $"Product={line.ProductId}, " +
                $"GKey={line.GKey}");
        }

        Header.Lines =
            new ObservableCollection<CustomerOrderLine>(lines);
    

    }

    private async Task FetchAssociatedCustomer()
    {
        var args = new EditValueChangedEventArgs("", CustomerPhoneNumber);
        await FetchCustomerCommand.ExecuteAsync(args);
    }


    [RelayCommand]
    private async Task FetchCustomerOrder(EditValueChangedEventArgs args)
    {
        if (args.NewValue is not string searchText || string.IsNullOrWhiteSpace(searchText) || searchText.Length < 8)
            return;

        try
        {
            createCustomer = false;

            Messenger.Default.Send(MessageType.WaitIndicator, WaitIndicatorVM.ShowIndicator("Fetching Order details..."));

            var header = await _customerOrderService.GetCustomerOrder(searchText.Trim());

            if (header == null)
            {
                _messageBoxService.ShowMessage("Order details not found.", "Order not found", MessageButton.OK);
                return;
            }

            Header = header;
            CustomerPhoneNumber = Header.CustMobileNbr;

            await PopulateOrderLines();
            await FetchAssociatedCustomer();
            await ResolveOrderStatusAsync();
            SelectedRows = Header.Lines;
            EvaluateForAllLines();
        }
        catch (Exception ex)
        {
            _messageBoxService.ShowMessage("Failed to fetch order: " + ex.Message, "Error", MessageButton.OK, MessageIcon.Error);
        }
        finally
        {
            Messenger.Default.Send(MessageType.WaitIndicator, WaitIndicatorVM.HideIndicator());
        }
    }

    [RelayCommand]
    private void ViewOrderSummary()
    {
        if (Header is null)
            return;

        _dialogService.ShowOrderSummary(Header);
    }


    [RelayCommand]
    private void Focus(TextEdit sender)
    {
        sender.Focus();
    }


    [RelayCommand]
    private void ResetCustomerOrder()
    {
        SetHeader();
        _ = SetThisCompany();
        //SetMasterLedger();
        Buyer = null;
        CustomerPhoneNumber = null;
        CustomerState = null;
        //SalesPerson = null;
        //invBalanceChk = false;  //reset to false for next invoice
    }

    private bool CanDeleteRows()
    {
        return SelectedRows?.Any() ?? false;
    }

    [RelayCommand(CanExecute = nameof(CanDeleteSingleRow))]
    private void DeleteSingleRow(CustomerOrderLine line)
    {
        var result = _messageBoxService.ShowMessage("Delete current row", "Delete Row", MessageButton.YesNo, MessageIcon.Question, MessageResult.No);

        if (result == MessageResult.No)
            return;

        var index = Header.Lines.Remove(line);
    }

    private bool CanDeleteSingleRow(CustomerOrderLine line)
    {
        return line is not null && Header.Lines.IndexOf(line) > -1;
    }

    private bool ValidateCustomerOrder()
    {
        ValidationErrors.Clear();

        if (Buyer is null)
        {
            ValidationErrors.Add(
                "Customer details are required.");
        }
        else
        {
            if (string.IsNullOrWhiteSpace(Buyer.MobileNbr))
            {
                ValidationErrors.Add(
                    "Customer mobile number is required.");
            }

            if (string.IsNullOrWhiteSpace(Buyer.CustomerName))
            {
                ValidationErrors.Add(
                    "Customer name is required.");
            }

            if (Buyer.Address is null ||
                string.IsNullOrWhiteSpace(Buyer.Address.State))
            {
                ValidationErrors.Add(
                    "Customer state is required.");
            }
        }

        if (Header is null)
        {
            ValidationErrors.Add(
                "Customer order information is not available.");

            HasValidationErrors = true;
            return false;
        }

        if (Header.Lines is null ||
            Header.Lines.Count == 0)
        {
            ValidationErrors.Add(
                "Add at least one ornament to the order.");
        }
        else
        {
            var lineNumber = 0;

            foreach (var line in Header.Lines)
            {
                lineNumber++;

                var prefix = $"Line {lineNumber}";

                if (string.IsNullOrWhiteSpace(line.ProductId))
                {
                    ValidationErrors.Add(
                        $"{prefix}: Product is required.");
                }

                if (line.ProdQty <= 0)
                {
                    ValidationErrors.Add(
                        $"{prefix}: Quantity must be greater than zero.");
                }

                var gross =
                    line.ProdGrossWeight ?? 0M;

                var stone =
                    line.ProdStoneWeight ?? 0M;

                var net =
                    line.ProdNetWeight ?? 0M;

                if (gross <= 0M)
                {
                    ValidationErrors.Add(
                        $"{prefix}: Gross weight must be greater than zero.");
                }

                if (stone > gross)
                {
                    ValidationErrors.Add(
                        $"{prefix}: Stone weight cannot exceed gross weight.");
                }

                if (net <= 0M)
                {
                    ValidationErrors.Add(
                        $"{prefix}: Net weight must be greater than zero.");
                }

                if ((line.MetalRate ?? 0M) <= 0M)
                {
                    ValidationErrors.Add(
                        $"{prefix}: Metal rate is not available.");
                }

                if (string.IsNullOrWhiteSpace(line.OrderType))
                {
                    ValidationErrors.Add(
                        $"{prefix}: Order type is required.");
                }

                if ((line.OrderAmount ?? 0M) <= 0M)
                {
                    ValidationErrors.Add(
                        $"{prefix}: Estimated amount could not be calculated.");
                }
            }
        }

        HasValidationErrors =
            ValidationErrors.Count > 0;

        return !HasValidationErrors;
    }

    [RelayCommand]
    private async Task FetchCustomer(
    EditValueChangedEventArgs args)
    {
        if (args.NewValue is not string phoneNumber)
            return;

        phoneNumber = phoneNumber.Trim();

        if (string.IsNullOrWhiteSpace(phoneNumber) ||
            phoneNumber.Length < 10)
        {
            return;
        }

        if (Buyer is not null &&
            Buyer.MobileNbr == phoneNumber &&
            Buyer.GKey > 0)
        {
            return;
        }

        try
        {
            CustomerReadOnly = false;
            createCustomer = false;

            Messenger.Default.Send(
                MessageType.WaitIndicator,
                WaitIndicatorVM.ShowIndicator(
                    "Fetching Customer details..."));

            var result =
                await _customerLookupService
                    .ResolveByMobileAsync(phoneNumber);

            Buyer = result.Customer;

            Buyer.Address ??= new OrgAddress();

            if (result.IsExisting)
            {
                await PrepareExistingCustomerAsync();

                return;
            }

            await PrepareNewCustomerAsync(phoneNumber);
        }
        catch (Exception ex)
        {
            _messageBoxService.ShowMessage(
                "Failed to fetch customer: " + ex.Message,
                "Customer Error",
                MessageButton.OK,
                MessageIcon.Error);
        }
        finally
        {
            Messenger.Default.Send(
                MessageType.WaitIndicator,
                WaitIndicatorVM.HideIndicator());
        }
    }

    private async Task PrepareExistingCustomerAsync()
    {
        if (Buyer is null)
            return;

        Buyer.Address ??= new OrgAddress();

        createCustomer = false;

        //
        // Existing customer is display-only by default.
        //
        CustomerReadOnly = true;

        //
        // Determine GST state.
        //
        var gstCode =
            Buyer.Address.GstStateCode;

        if (string.IsNullOrWhiteSpace(gstCode))
        {
            gstCode = Buyer.GstStateCode;
        }

        //
        // If customer has no GST state/location,
        // fall back to seller/company state.
        //
        if (string.IsNullOrWhiteSpace(gstCode))
        {
            gstCode = Company?.GstCode;

            Buyer.Address.GstStateCode = gstCode;
        }

        if (!string.IsNullOrWhiteSpace(gstCode))
        {
            CustomerState =
                await _referenceLoader.GetValueAsync(
                    "CUST_STATE",
                    gstCode);
        }

        //
        // Customer Order references the customer through GKey.
        //
        Header.CustGkey = Buyer.GKey;

        Header.CustMobileNbr =
            Buyer.MobileNbr;

        //
        // Re-evaluate transaction calculations if required.
        //
        EvaluateForAllLines();

        await ResolveOrderStatusAsync();

        //
        // Existing customer:
        // continue directly to transaction entry.
        //
        Messenger.Default.Send(
            "ProductIdUIName",
            MessageType.FocusTextEdit);
    }

    private async Task PrepareNewCustomerAsync(
        string phoneNumber)
    {
        if (Buyer is null)
            return;

        Buyer.Address ??=
            new OrgAddress();

        Buyer.MobileNbr =
            phoneNumber;

        createCustomer = true;
        CustomerReadOnly = false;

        if (Company is not null)
        {
            Buyer.Address.GstStateCode =
                Company.GstCode;

            Buyer.Address.State =
                Company.State;

            Buyer.Address.District =
                Company.District;

            Buyer.GstStateCode =
                Company.GstCode;
        }

        Header.CustGkey = 0;

        await OpenNewCustomerEditorAsync();
    }


    [RelayCommand]
    private async Task FetchProduct()
    {

        if (string.IsNullOrEmpty(ProductIdUI)) return;

        try
        {
            var productStk = await _productViewService.GetProduct(ProductIdUI);
            if (productStk is null)
            {
                _messageBoxService.ShowMessage($"No Product found for {ProductIdUI}", "Product not found", MessageButton.OK);
                return;
            }

            var billedPrice = _settingsPageViewModel.GetPrice(productStk.Metal);

            var custOrdLine = new CustomerOrderLine
            {
                ProdQty = 1,
                MetalRate = billedPrice,
                OrderType = "New Making"
            };

            custOrdLine.SetProductDetails(productStk);

            EvaluateFormula(custOrdLine, isInit: true);

            Header.Lines.Add(custOrdLine);
        }
        catch (Exception ex)
        {
            _messageBoxService.ShowMessage("Error fetching product: " + ex.Message);
        }
    }

    /*   partial void OnOrderStatusUIChanged(string oldValue, string newValue)
       {
           Header.OrderStatusFlag = Int32.Parse(GetOrderStatus(0, newValue));
       }*/

    partial void OnCustomerStateChanged(string value)
    {
        _ = ApplyCustomerStateAsync(value);
    }

    private async Task ApplyCustomerStateAsync(
    string stateName)
    {
        if (Buyer is null ||
            string.IsNullOrWhiteSpace(stateName))
        {
            return;
        }

        try
        {
            Buyer.Address ??=
                new OrgAddress();

            var gstStateCode =
                await _referenceLoader.GetCodeAsync(
                    "CUST_STATE",
                    stateName);

            Buyer.Address.GstStateCode =
                gstStateCode;

            //
            // Compatibility property.
            //
            Buyer.GstStateCode =
                gstStateCode;

            EvaluateForAllLines();

            await ResolveOrderStatusAsync();
        }
        catch (Exception ex)
        {
            _messageBoxService.ShowMessage(
                "Unable to resolve customer state: " +
                ex.Message,
                "Customer State",
                MessageButton.OK,
                MessageIcon.Error);
        }
    }

    private void EvaluateForAllLines()
    {
        if (Header?.Lines is null)
            return;

        foreach (var line in Header.Lines)
        {
            CalculateCustomerOrderLine(line);
        }

        RecalculateHeaderTotals();
    }


    private async Task ResolveOrderStatusAsync()
    {
        if (Header is null)
            return;

        OrderStatusUI =
            await _referenceLoader.GetCodeAsNameAsync(
                "CUST_ORD_STATUS",
                Header.OrderStatusFlag.ToString());
    }

    private void EvaluateFormula<T>(T item, bool isInit = false) where T : class
    {
        var formulas = FormulaStore.Instance.GetFormulas<T>();

        foreach (var formula in formulas)
        {

            var val = formula.Evaluate<T, decimal>(item, 0M);

            //if (item is CustomerOrderLine custOrdLine)
            //    copyCustomerOrderLineExpression[formula.FieldName].Invoke(custOrdLine, val);


            if (item is CustomerOrderLine custOrdLine &&
                copyCustomerOrderLineExpression.TryGetValue(formula.FieldName, out var setter))
            {
                setter.Invoke(custOrdLine, val);
            }
        }
    }

    [RelayCommand]
    private async Task CellUpdate(
        CellValueChangedEventArgs args)
    {

        if (HasValidationErrors)
        {
            ValidationErrors.Clear();
            HasValidationErrors = false;
        }

        if (args.Row is CustomerOrderLine line)
        {
            CalculateCustomerOrderLine(line);

            RecalculateHeaderTotals();
        }
        else if (args.Row is LedgersTransactions receiptLine)
        {
            EvaluateArRctLine(receiptLine);

            RecalculateHeaderTotals();
        }
        else if (
            args.Row is OldMetalTransaction oldMetalTransaction &&
            args.Column.FieldName !=
                nameof(OldMetalTransaction.FinalPurchasePrice))
        {
            await EvaluateOldMetalTransactionsAsync(
                oldMetalTransaction);

            RecalculateHeaderTotals();
        }
    }

    private void CalculateCustomerOrderLine(
    CustomerOrderLine line)
    {
        if (line is null)
            return;

        // ---------------------------------------------------------
        // 1. WEIGHT
        // ---------------------------------------------------------

        var grossWeight =
            line.ProdGrossWeight ?? 0M;

        var stoneWeight =
            line.ProdStoneWeight ?? 0M;

        line.ProdNetWeight =
            Math.Max(
                0M,
                grossWeight - stoneWeight);


        // ---------------------------------------------------------
        // 2. METAL VALUE
        // ---------------------------------------------------------

        var netWeight =
            line.ProdNetWeight ?? 0M;

        var metalRate =
            line.MetalRate ?? 0M;

        var metalValue =
            netWeight * metalRate;


        // ---------------------------------------------------------
        // 3. VA
        // ---------------------------------------------------------

        var vaPercent =
            line.VaPercent ?? 0M;

        line.VaAmount =
            metalValue * vaPercent / 100M;


        // ---------------------------------------------------------
        // 4. MAKING CHARGES
        //
        // Keep whatever MakingCharges the user/formula has supplied.
        // Do NOT overwrite it here yet.
        // ---------------------------------------------------------

        var makingCharges =
            line.MakingCharges ?? 0M;


        // ---------------------------------------------------------
        // 5. TAXABLE VALUE
        // ---------------------------------------------------------

        var taxableAmount =
            metalValue +
            (line.VaAmount ?? 0M) +
            makingCharges;


        // ---------------------------------------------------------
        // 6. TAX
        //
        // TEMPORARILY use 3% only if that is your existing
        // Customer Order GST rule.
        // ---------------------------------------------------------

        const decimal taxPercent = 3M;

        line.TaxAmount =
            taxableAmount * taxPercent / 100M;


        // ---------------------------------------------------------
        // 7. ESTIMATED ORDER AMOUNT
        // ---------------------------------------------------------

        line.OrderAmount =
            taxableAmount +
            (line.TaxAmount ?? 0M);


        // Optional monetary rounding
        line.VaAmount =
            Math.Round(
                line.VaAmount ?? 0M,
                2,
                MidpointRounding.AwayFromZero);

        line.TaxAmount =
            Math.Round(
                line.TaxAmount ?? 0M,
                2,
                MidpointRounding.AwayFromZero);

        line.OrderAmount =
            Math.Round(
                line.OrderAmount ?? 0M,
                2,
                MidpointRounding.AwayFromZero);
    }

    [RelayCommand]
    private void EvaluateArRctLine(LedgersTransactions orderRctLines)
    {

    }

    private void RecalculateHeaderTotals()
    {
        if (Header is null)
            return;

        var lines = Header.Lines?
            .Where(x => x is not null)
            .ToList()
            ?? new List<CustomerOrderLine>();

        // ---------------------------------------------------------
        // ITEMS
        // ---------------------------------------------------------

        Header.OrderedItems =
            lines.Sum(x => x.ProdQty);


        // ---------------------------------------------------------
        // WEIGHTS
        // ---------------------------------------------------------

        Header.TotalGrossWeight =
            lines.Sum(x => x.ProdGrossWeight ?? 0M);

        Header.TotalStoneWeight =
            lines.Sum(x => x.ProdStoneWeight ?? 0M);

        Header.TotalNetWeight =
            lines.Sum(x => x.ProdNetWeight ?? 0M);


        // ---------------------------------------------------------
        // CHARGES
        // ---------------------------------------------------------

        Header.TotalMakingCharges =
            lines.Sum(x => x.MakingCharges ?? 0M);

        Header.TotalTaxAmount =
            lines.Sum(x => x.TaxAmount ?? 0M);

        Header.TotalOrderAmount =
            lines.Sum(x => x.OrderAmount ?? 0M);


        // ---------------------------------------------------------
        // OLD METAL
        // ---------------------------------------------------------

        if (Header.OldMetalTransactions is not null)
        {
            Header.OldMetalNetWeight =
                Header.OldMetalTransactions
                    .Sum(x => x.NetWeight ?? 0M);
        }
        else
        {
            Header.OldMetalNetWeight = 0M;
        }


        // ---------------------------------------------------------
        // ADVANCE
        // ---------------------------------------------------------

        if (Header.AdvanceReceiptLines is not null)
        {
            Header.AdvancePaidAmount =
                Header.AdvanceReceiptLines
                    .Sum(x => x.TransactionAmount);
        }
        else
        {
            Header.AdvancePaidAmount = 0M;
        }


        // ---------------------------------------------------------
        // BALANCE
        // ---------------------------------------------------------

        Header.BalanceAmount =
            (Header.TotalOrderAmount ?? 0M) -
            (Header.AdvancePaidAmount ?? 0M);
    }

    private void AssignLineNumbers()
    {
        for (int i = 0; i < Header.Lines.Count; i++)
        {
            var line = Header.Lines[i];
            line.OrderLineNbr = i + 1;
            line.OrderNbr = Header.OrderNbr;
        }
    }

    private bool IsNewOrder()
    {
        if (Header?.GKey > 0)
            return false;
        else
            return true;
    }


    private Task EnsureCustomerSavedAsync()
    {
        if (Buyer is null)
        {
            throw new InvalidOperationException(
                "Customer information is missing.");
        }

        if (Buyer.GKey <= 0)
        {
            throw new InvalidOperationException(
                "Please create/save the customer before saving the order.");
        }

        Header.CustGkey =
            Buyer.GKey;

        Header.CustMobileNbr =
            Buyer.MobileNbr;

        return Task.CompletedTask;
    }


    private async Task SaveNewOrderAsync()
    {

        Header.OrderStatusFlag =
            await _referenceLoader.GetCodeAsIntAsync(
                "CUST_ORD_STATUS",
                OrderStatusUI);

        var request =
            CustomerOrderRequestMapper.ToSaveRequest(Header);

        var result =
            await _customerOrderService.SaveAsync(request);

        if (result is null)
        {
            throw new InvalidOperationException(
                "Customer order save returned no result.");
        }

        Header.GKey = result.Gkey;
        Header.OrderNbr = result.OrderNbr;



    }

    private async Task UpdateOrderAsync()
    {
       
        Header.OrderStatusFlag = await _referenceLoader.GetCodeAsIntAsync("CUST_ORD_STATUS", OrderStatusUI);
     
        await _customerOrderService.UpdateHeader(Header);

    }

    private bool PrepareAndValidateOrder()
    {
        ValidationErrors.Clear();
        HasValidationErrors = false;

        EvaluateForAllLines();

        // EvaluateForAllLines already calls
        // RecalculateHeaderTotals() in our latest version.

        return ValidateCustomerOrder();
    }

    [RelayCommand]
    private async Task CreateCustomerOrder()
    {
        try
        {

            // -----------------------------------------------------
            // CALCULATE + VALIDATE FIRST
            // -----------------------------------------------------

            if (!PrepareAndValidateOrder())
                return;

            await EnsureCustomerSavedAsync();

            Header.CustGkey = Buyer?.GKey;


            AssignLineNumbers();

            if (IsNewOrder())
            {
                await SaveNewOrderAsync();
            }
            else
            {
                await SetOrderStatusAsync();
                await UpdateOrderAsync();
            }

            _messageBoxService.ShowMessage(
                $"Customer Order {Header.OrderNbr} {(IsNewOrder() ? "Created" : "Updated")} Successfully",
                "Customer Order",
                MessageButton.OK,
                MessageIcon.Exclamation
            );

            Messenger.Default.Send(MessageType.WaitIndicator, WaitIndicatorVM.HideIndicator());
            ResetCustomerOrder();
        }
        catch (Exception ex)
        {

            ValidationErrors.Clear();

            ValidationErrors.Add(
                $"Failed to process order: {ex.Message}");

            HasValidationErrors = true;

        }
    }

    private async Task SetOrderStatusAsync()
    {
        Header.OrderStatusFlag = await _referenceLoader.GetCodeAsIntAsync("CUST_ORD_STATUS", OrderStatusUI);
    }

    /*    
        private async Task CreateCustomerOrderOld()
        {
                *//* Messenger.Default.Send(MessageType.WaitIndicator, WaitIndicatorVM.ShowIndicator("Print Invoice..."));
                PrintPreviewInvoice();
                PrintPreviewInvoiceCommand.NotifyCanExecuteChanged();
                PrintInvoiceCommand.NotifyCanExecuteChanged();*//*
                Messenger.Default.Send(MessageType.WaitIndicator, VM.ShowIndicator("Fetching Order details..."));

                ResetCustomerOrder();
            }
        }*/

    private bool CanCreateCustomerOrder()
    {
        return string.IsNullOrEmpty(Header?.OrderNbr);
    }

    [RelayCommand]
    private async Task EvaluateOldMetalTransactionsAsync(OldMetalTransaction oldMetalTransaction)
    {

        /*        if (oldMetalTransaction.ProductId is null)
                {
                    return;
                }*/

        if (string.IsNullOrEmpty(oldMetalTransaction.ProductId)) return;

        OldMetalProductView = await _productViewService.GetProduct(oldMetalTransaction.ProductId);

        if (OldMetalProductView is null)
        {
            _messageBoxService.ShowMessage($"No Product found for {OldMetalProductView}, Please make sure it exists",
                "Product not found", MessageButton.OK, MessageIcon.Error);
            return;
        }

        var metalPrice = _settingsPageViewModel.GetPrice("GOLD");

        if (metalPrice < 1)
        {
            displayRateErrorMsg();
            //return;
        }

        oldMetalTransaction.TransactedRate = todaysRate;


        if (oldMetalTransaction.TransactedRate.GetValueOrDefault() < 1)
            oldMetalTransaction.TransactedRate = metalPrice; // todaysRate;

        oldMetalTransaction.Purity = OldMetalProductView.Purity;


        oldMetalTransaction.NetWeight = (
                                           oldMetalTransaction.GrossWeight.GetValueOrDefault() -
                                           oldMetalTransaction.StoneWeight.GetValueOrDefault() -
                                           oldMetalTransaction.WastageWeight.GetValueOrDefault()
                                        );

        oldMetalTransaction.TotalProposedPrice = oldMetalTransaction.NetWeight.GetValueOrDefault() *
                                                    oldMetalTransaction.TransactedRate.GetValueOrDefault();
        oldMetalTransaction.FinalPurchasePrice = oldMetalTransaction.TotalProposedPrice;

        oldMetalTransaction.DocRefType = "Invoice";

        oldMetalTransaction.EnrichOldMetalProductDetails(OldMetalProductView);


    }


    [RelayCommand]
    private Task EvaluateOldMetalTransaction(OldMetalTransaction oldMetalTransaction)
    {

        OldMetalTransaction oldMetalTransactionLine = new OldMetalTransaction()
        {
            CustGkey = Header.CustGkey,
            CustMobile = Header.CustMobileNbr,
            TransDate = DateTime.Now,
            Uom = "Grams",
            TransType = "OG Purchase"
        };

        Header.OldMetalTransactions.Add(oldMetalTransactionLine);

        return Task.CompletedTask;

    }

    private async Task ProcessOldMetalTransaction()
    {

        foreach (var omTrans in Header.OldMetalTransactions)
        {
            omTrans.EnrichCustOrderDetails(Header);
        }

        await _oldMetalTransactionService.CreateOldMetalTransaction(Header.OldMetalTransactions);
    }


    private async Task ProcessReceipts()
    {
        //For each Receipts row - seperate Voucher has to be created
        foreach (var receipts in Header.AdvanceReceiptLines)
        {
            if (receipts is null) return;

            var voucher = CreateVoucher(receipts);
            voucher = await SaveVoucher(voucher);

            /*            var arReceipts = CreateArReceipts(receipts, voucher);
                        await SaveArReceipts(arReceipts);*/

        }
    }

    [RelayCommand] //(CanExecute = nameof(CanProcessArReceipts))]
    private void ProcessAdvReceipts()
    {
        //  var paymentMode = await _mtblReferencesService.GetReference("PAYMENT_MODE");

        //var noOfLines = Header.AdvanceReceiptLines.Count;

        Header.AdvanceReceiptLines.Add(new LedgersTransactions
        {
            TransactionDate = DateTime.Today,
            DrCr = "Dr"
        });

    }

    private async Task SaveLedgerTransactions()
    {

        //check customer has already ledger entry
        LedgerHeader = await _ledgerService.GetHeader(MtblLedger.GKey, Buyer.GKey);

        if (LedgerHeader is null)
        {
            LedgerHeader = new();

            LedgerHeader.MtblLedgersGkey = MtblLedger.GKey;
            LedgerHeader.CustGkey = Header.CustGkey;
            LedgerHeader.BalanceAsOn = DateTime.Now;

            LedgerHeader.CurrentBalance = 0; // Header.AdvanceAdj.GetValueOrDefault();

            LedgerHeader = await _ledgerService.CreateHeader(LedgerHeader);
        }

        foreach (var trx in Header.AdvanceReceiptLines)
        {
            trx.LedgerHdrGkey = LedgerHeader.GKey;
            trx.DocumentNbr = Header.OrderNbr;
            trx.DocumentDate = Header.OrderDate;

            await _ledgerService.CreateLedgersTransactions(trx);
        }

        //_messageBoxService.ShowMessage("Ledger transactions saved successfully.");
    }

    private async Task OpenNewCustomerEditorAsync()
    {
        if (Buyer is null)
            return;

        var savedCustomer =
            await _dialogService.EditCustomerAsync(
                Buyer,
                isNewCustomer: true);

        // User cancelled.
        if (savedCustomer is null)
        {
            Header.CustGkey = 0;
            createCustomer = false;
            CustomerReadOnly = false;

            return;
        }

        if (savedCustomer.GKey <= 0)
        {
            throw new InvalidOperationException(
                "Customer was saved but no valid GKey was returned.");
        }

        Buyer = savedCustomer;
        Buyer.Address ??= new OrgAddress();

        Header.CustGkey = Buyer.GKey;
        Header.CustMobileNbr = Buyer.MobileNbr;

        createCustomer = false;
        CustomerReadOnly = true;

        var gstCode =
            Buyer.Address.GstStateCode
            ?? Buyer.GstStateCode
            ?? Company?.GstCode;

        if (!string.IsNullOrWhiteSpace(gstCode))
        {
            Buyer.Address.GstStateCode = gstCode;
            Buyer.GstStateCode = gstCode;

            CustomerState =
                await _referenceLoader.GetValueAsync(
                    "CUST_STATE",
                    gstCode);
        }

        EvaluateForAllLines();
        ResolveOrderStatusAsync();

        Messenger.Default.Send(
            "ProductIdUIName",
            MessageType.FocusTextEdit);
    }


    private Voucher CreateVoucher(LedgersTransactions advLdgrTrans)
    {

        Voucher Voucher = new()
        {
            VoucherDate = DateTime.Now
        };

        Voucher.SeqNbr = 1;
        Voucher.CustomerGkey = Header.CustGkey;
        Voucher.VoucherDate = DateTime.Now;
        Voucher.TransType = "Receipt";         // Trans_type    1 = Receipt,    2 = Payment,    3 = Journal
        Voucher.VoucherType = "Advance Receipt"; // Voucher_type  1 = Sales,      2 = Credit,     3 = Expense
        Voucher.Mode = advLdgrTrans.TransType;
        Voucher.TransDate = DateTime.Now;
        Voucher.VoucherNbr = Header.OrderNbr;
        Voucher.RefDocNbr = Header.OrderNbr;
        Voucher.RefDocDate = Header.OrderDate;
        Voucher.RefDocGkey = Header.GKey;
        Voucher.TransAmount = advLdgrTrans.TransactionAmount;
        Voucher.TransDesc = Voucher.VoucherType + "-" + Voucher.TransType + "-" + Voucher.Mode;

        return Voucher;

    }

    private async Task<Voucher> SaveVoucher(Voucher voucher)
    {
        if (voucher.GKey == 0)
        {
            var voucherResult = await _voucherService.CreateVoucher(voucher);

            if (voucherResult != null)
            {
                voucher = voucherResult;
                //  _messageBoxService.ShowMessage("Voucher Created Successfully", "Voucher Created",
                //      MessageButton.OK, MessageIcon.Exclamation);
            }
        }
        else
        {
            await _voucherService.UpdateVoucher(voucher);
        }

        return voucher;

    }
}
