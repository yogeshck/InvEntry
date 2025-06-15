using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DevExpress.Charts.Designer.Native;
using DevExpress.Mvvm;
using DevExpress.Mvvm.Native;
using DevExpress.Xpf.Core;
using DevExpress.Xpf.Core.Native;
using DevExpress.Xpf.Editors;
using DevExpress.Xpf.Grid;
using DevExpress.Xpf.Printing;
using InvEntry.Extension;
using InvEntry.Helper;
using InvEntry.Models;
using InvEntry.Models.Extensions;
using InvEntry.Reports;
using InvEntry.Services;
using InvEntry.Store;
using InvEntry.Utils;
using InvEntry.Utils.Options;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections;
using System.Collections.Specialized;
using System.Drawing.Text;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using IDialogService = DevExpress.Mvvm.IDialogService;
using DevExpress.CodeParser;

namespace InvEntry.ViewModels;

public partial class CustomerOrderViewModel : ObservableObject
{
    [ObservableProperty]
    private string _customerPhoneNumber;

    [ObservableProperty]
    private MtblReference _customerState;

    [ObservableProperty]
    private MtblReference _salesPerson;

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
    private ObservableCollection<CustomerOrderLine> selectedRows;

    /*    [ObservableProperty]
        private ObservableCollection<ProductView> productStockList;*/

    [ObservableProperty]
    private ObservableCollection<string> productCategoryList;

    [ObservableProperty]
    private ObservableCollection<string> metalList;

    [ObservableProperty]
    private ObservableCollection<MtblReference> custOrdStatusList; 

    [ObservableProperty]
    private ObservableCollection<MtblReference> mtblReferencesList;

    [ObservableProperty]
    private ObservableCollection<MtblReference> salesPersonReferencesList;

    [ObservableProperty]
    private ObservableCollection<MtblReference> stateReferencesList;

    [ObservableProperty]
    private string _searchText;

    [ObservableProperty]
    private DateSearchOption _searchOption;

    private bool createCustomer = false;
    private bool updateOrder = false;
    private bool invBalanceChk = false;

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
    private SettingsPageViewModel _settingsPageViewModel;
    private Dictionary<string, Action<CustomerOrderLine, decimal?>> copyCustomerOrderLineExpression;
    private Dictionary<string, Action<CustomerOrder, decimal?>> copyCustomerOrderExpression;
    //private Dictionary<int, string> orderStatus = new Dictionary<int, string>();
    //private Dictionary<string, MtblReference> dictionaryOrderStatus = new Dictionary<string, MtblReference>();


    public CustomerOrderViewModel(

            ICustomerService customerService,
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
            [FromKeyedServices("ReportDialogService")] IDialogService reportDialogService)
    {
        // Assign dependencies
        _orgThisCompanyViewService = orgThisCompanyViewService;
        _customerService = customerService;
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

        selectedRows = new();
        _customerReadOnly = false;
        _isBalance = true;
        _isRefund = false;

        // Start async init
        _ = InitializeAsync();

        SetHeader();
 

/*        PopulateProductCategoryList();
        PopulateStateList();
        PopulateUnboundLineDataMap();
        PopulateMtblRefNameList();
        PopulateMetalList();
        PopulateOrderStatusList();
     //   PopulateTaxList();
        PopulateSalesPersonList();*/

        //PopulateUnboundHeaderDataMap();
    }

    private async Task InitializeAsync()
    {
        try
        {
            await SetThisCompany();
            SetHeader(); // Not async – keep as is
            await SetMasterLedger();
            await PopulateProductCategoryList();
            await PopulateStateList();
            await PopulateMtblRefNameList();
            await PopulateMetalList();
            await PopulateOrderStatusList();
            await PopulateSalesPersonList();
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

    private async Task PopulateProductCategoryList()
    {
        var list = await _productCategoryService.GetProductCategoryList();
        ProductCategoryList = new(list.Select(x => x.Name));
    }

    private async Task PopulateStateList()
    {
        var stateRefList = new List<MtblReference>();

        var stateRefServiceList = await _mtblReferencesService.GetReferenceList("CUST_STATE");

        if (stateRefServiceList is null)
        {
            stateRefList.Add(new MtblReference() { RefValue = "Tamil Nadu", RefCode = "33" });
            stateRefList.Add(new MtblReference() { RefValue = "Kerala", RefCode = "32" });
            stateRefList.Add(new MtblReference() { RefValue = "Karnataka", RefCode = "30" });
        }
        else
        {
            stateRefList.AddRange(stateRefServiceList);
        }

        StateReferencesList = new(stateRefList);

        // CustomerState = StateReferencesList.FirstOrDefault(x => x.RefCode.Equals(Company.GstCode));
    }

    private async Task PopulateSalesPersonList()
    {
        var salesPersonRefList = await _mtblReferencesService.GetReferenceList("SALES_PERSON");

        if (salesPersonRefList is null)
        {
            SalesPersonReferencesList = new();
            SalesPersonReferencesList.Add(new MtblReference() { RefValue = "Ebi", RefCode = "33" }); //Yet To fix
            SalesPersonReferencesList.Add(new MtblReference() { RefValue = "Vinnila", RefCode = "32" });
            SalesPersonReferencesList.Add(new MtblReference() { RefValue = "Anju", RefCode = "30" });
            return;
        }

        SalesPersonReferencesList = new(salesPersonRefList);

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

    private async Task PopulateOrderStatusList()
    {
        var ordStatusRefLst = await _mtblReferencesService.GetReferenceList("CUST_ORD_STATUS");
        CustOrdStatusList = new(ordStatusRefLst);
    }

    
    private string GetOrderStatus(int? statusCode, string statusName)
    {
        var ordStatus = ""; 

        if (statusCode > 0)
        {
            ordStatus = CustOrdStatusList.FirstOrDefault(x => int.TryParse(x.RefCode, out var code) && code == statusCode).RefValue;
        } else if (statusName is not null)
        {
            ordStatus = CustOrdStatusList.FirstOrDefault(x => x.RefValue == statusName).RefCode;
        }
            return ordStatus;
    }

    private async Task PopulateOrderLines()
    {
        var lines = await _customerOrderService.GetLines(Header.OrderNbr);
        Header.Lines = new ObservableCollection<CustomerOrderLine>(lines);
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
            EvaluateHeader();
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


/*    [RelayCommand]
    private async Task FetchCustomerOrderOld(EditValueChangedEventArgs args)
    {
        if (args.NewValue is not string searchText) return;

        searchText = searchText.Trim();

        if (string.IsNullOrEmpty(searchText) || searchText.Length < 8)
            return;

        createCustomer = false;

        Messenger.Default.Send(MessageType.WaitIndicator, WaitIndicatorVM.ShowIndicator("Fetching Order details..."));

        Header = await _customerOrderService.GetCustomerOrder(searchText);

        Messenger.Default.Send(MessageType.WaitIndicator, WaitIndicatorVM.HideIndicator());

        if (Header is null)
        {
            _messageBoxService.ShowMessage(" Order details not found.", "Order not found", MessageButton.OK);
            return;
        }

        CustomerPhoneNumber = Header.CustMobileNbr;
        EvaluateHeader();
         
        var custOrdLines =  await _customerOrderService.GetLines(Header.OrderNbr);

        Header.Lines = new (custOrdLines);

        var args1 = new EditValueChangedEventArgs("", CustomerPhoneNumber);
        await FetchCustomerCommand.ExecuteAsync(args1);

        SelectedRows = Header.Lines;

        //orderLinesUIGrid.ItemsSource = custOrdLines;

        EvaluateForAllLines();

        //FetchCustomer(CustomerPhoneNumber);
    }*/

    private async Task PopulateMtblRefNameList()
    {
        var mtblRefList = await _mtblReferencesService.GetReferenceList("PAYMENT_MODE");
        MtblReferencesList = new(mtblRefList);
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
        SalesPerson = null;
        CreateCustomerOrderCommand.NotifyCanExecuteChanged();
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

    [RelayCommand]
    private async Task FetchCustomer(EditValueChangedEventArgs args)
    {
        if (args.NewValue is not string phoneNumber) return;

        phoneNumber = phoneNumber.Trim();

        if (string.IsNullOrEmpty(phoneNumber) || phoneNumber.Length < 10)
            return;

        if (Buyer is not null && Buyer.MobileNbr == phoneNumber)
            return;

        CustomerReadOnly = false;
        createCustomer = false;

        Messenger.Default.Send(MessageType.WaitIndicator, WaitIndicatorVM.ShowIndicator("Fetching Customer details..."));

        Buyer = await _customerService.GetCustomer(phoneNumber);

        Messenger.Default.Send(MessageType.WaitIndicator, WaitIndicatorVM.HideIndicator());

        if (Buyer is null)
        {
            _messageBoxService.ShowMessage("No customer details found.", "Customer not found", MessageButton.OK);

            Buyer = new();
            Buyer.MobileNbr = phoneNumber;
            Buyer.Address.GstStateCode = Company.GstCode;
            Buyer.Address.State = Company.State;
            Buyer.Address.District = Company.District;

            createCustomer = true;
            CustomerState = StateReferencesList.FirstOrDefault(x => x.RefCode == Company.GstCode);

            Messenger.Default.Send("CustomerNameUI", MessageType.FocusTextEdit);
        }
        else
        {
            var gstCode = Buyer.Address is null ? Company.GstCode : Buyer.Address.GstStateCode;

            if (Buyer.Address is null)
            {
                Buyer.Address = new();
                Buyer.Address.GstStateCode = Company.GstCode;
            }

            CustomerState = StateReferencesList.FirstOrDefault(x => x.RefCode == gstCode);

            Messenger.Default.Send("ProductIdUIName", MessageType.FocusTextEdit);

        }

        Header.CustMobileNbr = phoneNumber;
        EvaluateHeader();
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
                    MetalRate = billedPrice
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

    partial void OnCustomerStateChanged(MtblReference value)
    {
        if (Buyer is null) return;

        Buyer.GstStateCode = value.RefCode;

        //>>    Header.CgstPercent = GetGSTPercent("CGST");
        //>>    Header.SgstPercent = GetGSTPercent("SGST");
        //>>    Header.IgstPercent = GetGSTPercent("IGST");

        //Need to fetch based on pincode - future change
        //>>    Header.GstLocBuyer = value.RefCode;

        EvaluateForAllLines();
        //EvaluateHeader();
    }


    private void EvaluateForAllLines()
    {
        foreach (var line in Header.Lines)
        {
            EvaluateFormula(line);
        }
    }

    [RelayCommand]
    private void EvaluateHeader()
    {
        OrderStatusUI = GetOrderStatus(Header.OrderStatusFlag, "");
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
    private void CellUpdate(CellValueChangedEventArgs args)
    {
        if (args.Row is CustomerOrderLine line)
        {
            EvaluateFormula(line);
        }
        else
        if (args.Row is LedgersTransactions orderRctLines)
        {
            EvaluateArRctLine(orderRctLines);
        }
        else
        if (args.Row is OldMetalTransaction oldMetalTransaction &&
            args.Column.FieldName != nameof(OldMetalTransaction.FinalPurchasePrice))
        {
            EvaluateOldMetalTransactions(oldMetalTransaction);
        }

        EvaluateHeader();
    }

    [RelayCommand]
    private void EvaluateArRctLine(LedgersTransactions orderRctLines)
    {

    }

    private bool ValidateBeforeCreate()
    {
        if (Buyer is null || string.IsNullOrEmpty(Buyer.CustomerName))
        {
            _messageBoxService.ShowMessage("Customer information is missing.", "Validation Error", MessageButton.OK, MessageIcon.Warning);
            return false;
        }

        if (Header?.Lines == null || !Header.Lines.Any())
        {
            _messageBoxService.ShowMessage("No items in the order.", "Validation Error", MessageButton.OK, MessageIcon.Warning);
            return false;
        }

        return true;
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
         if (Header?.GKey > 0 )
              return false;
         else
              return true;
    }


    private async Task CreateOrUpdateCustomerAsync()
    {
        if (createCustomer)
        {
            Buyer = await _customerService.CreateCustomer(Buyer);
        }
    }


    private async Task SaveNewOrderAsync()
    {
        Header.OrderStatusFlag = int.Parse(GetOrderStatus(0, OrderStatusUI));

        var headerResult = await _customerOrderService.CreateCustomerOrder(Header);

        if (headerResult == null) throw new Exception("Failed to create customer order.");

        Header.GKey = headerResult.GKey;
        Header.OrderNbr = headerResult.OrderNbr;

        foreach (var line in Header.Lines)
        {
            line.OrderGkey = Header.GKey;
            line.OrderNbr = Header.OrderNbr;
            line.TenantGkey = Header.TenantGkey;
        }

        await _customerOrderService.CreateCustomerOrderLine(Header.Lines);

        await ProcessOldMetalTransaction();
        await ProcessReceipts();
        await SaveLedgerTransactions();
    }

    private async Task UpdateOrderAsync()
    {
        Header.OrderStatusFlag = int.Parse(GetOrderStatus(0, OrderStatusUI));
        await _customerOrderService.UpdateHeader(Header);
    }

    [RelayCommand]                           
    private async Task CreateCustomerOrder()
    {
        try
        {
            if (!ValidateBeforeCreate()) return;

            await CreateOrUpdateCustomerAsync();

            Header.CustGkey = Buyer?.GKey;

            AssignLineNumbers();

            if (IsNewOrder())
            {
                await SaveNewOrderAsync();
            }
            else
            {
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
            _messageBoxService.ShowMessage("Failed to process order: " + ex.Message, "Error", MessageButton.OK, MessageIcon.Error);
        }
    }


/*    
    private async Task CreateCustomerOrderOld()
    {
            *//* Messenger.Default.Send(MessageType.WaitIndicator, WaitIndicatorVM.ShowIndicator("Print Invoice..."));
            PrintPreviewInvoice();
            PrintPreviewInvoiceCommand.NotifyCanExecuteChanged();
            PrintInvoiceCommand.NotifyCanExecuteChanged();*//*
            Messenger.Default.Send(MessageType.WaitIndicator, WaitIndicatorVM.HideIndicator());

            ResetCustomerOrder();
        }
    }*/

    private bool CanCreateCustomerOrder()
    {
        return string.IsNullOrEmpty(Header?.OrderNbr);
    }

    [RelayCommand]
    private void EvaluateOldMetalTransactions(OldMetalTransaction oldMetalTransaction)
    {
        oldMetalTransaction.NetWeight = (
                                   oldMetalTransaction.GrossWeight.GetValueOrDefault() -
                                   oldMetalTransaction.StoneWeight.GetValueOrDefault() -
                                   oldMetalTransaction.WastageWeight.GetValueOrDefault()
                                );

        oldMetalTransaction.TotalProposedPrice = oldMetalTransaction.NetWeight.GetValueOrDefault() *
                                                    oldMetalTransaction.TransactedRate.GetValueOrDefault();
        oldMetalTransaction.FinalPurchasePrice = oldMetalTransaction.TotalProposedPrice;

    }


    [RelayCommand]
    private Task EvaluateOldMetalTransaction(OldMetalTransaction oldMetalTransaction)   
    {

        OldMetalTransaction oldMetalTransactionLine = new OldMetalTransaction()
        {
            CustGkey = Header.CustGkey,
            CustMobile = Header.CustMobileNbr,
            TransDate = DateTime.Now,
            Uom = "Grams"
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
            trx.DocumentNbr = trx.TransType;

            await _ledgerService.CreateLedgersTransactions(trx); 
        }

        //_messageBoxService.ShowMessage("Ledger transactions saved successfully.");
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
        Voucher.VoucherType = "Advance"; // Voucher_type  1 = Sales,      2 = Credit,     3 = Expense
        Voucher.Mode = advLdgrTrans.TransType;
        Voucher.TransDate =  DateTime.Now;
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
