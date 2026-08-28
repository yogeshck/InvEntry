using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DevExpress.Mvvm;
using DevExpress.Xpf.Editors;
using DevExpress.Xpf.Grid;
using InvEntry.Extension;
using InvEntry.Helpers;
using InvEntry.Models;
using InvEntry.Reports;
using InvEntry.Services;
using InvEntry.Services.Customers;
using InvEntry.Tally;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using System.Threading.Tasks;
using IDialogService = DevExpress.Mvvm.IDialogService;

namespace InvEntry.ViewModels;

public partial class OldMetalPurchaseViewModel : ObservableObject
{
    // ============================================================
    // DEPENDENCIES
    // ============================================================

    private readonly ICustomerLookupService _customerLookupService;
    private readonly IProductViewService _productViewService;
    private readonly IOldMetalTransactionService _oldMetalTransactionService;
    private readonly IOrgThisCompanyViewService _orgThisCompanyViewService;

    private readonly ReferenceLoader _referenceLoader;
    private readonly SettingsPageViewModel _settingsPageViewModel;

    private readonly IDialogService _dialogService;
    private readonly IDialogService _reportDialogService;
    private readonly IMessageBoxService _messageBoxService;


    // ============================================================
    // INTERNAL STATE
    // ============================================================

    private bool _initialized;
    private bool _createCustomer;

    private decimal _todaysRate;


    // ============================================================
    // CUSTOMER
    // ============================================================

    [ObservableProperty]
    private string? customerPhoneNumber;

    [ObservableProperty]
    private Customer? buyer;

    [ObservableProperty]
    private string? customerState;

    [ObservableProperty]
    private bool customerReadOnly;


    // ============================================================
    // COMPANY
    // ============================================================

    [ObservableProperty]
    private OrgThisCompanyView? company;


    // ============================================================
    // PURCHASE
    // ============================================================

    [ObservableProperty]
    private string? purchaseNumber;

    [ObservableProperty]
    private DateTime purchaseDate = DateTime.Now;

    public decimal TodaysRate =>
        _todaysRate;


    // ============================================================
    // PURCHASE STATE
    // ============================================================

    /// <summary>
    /// True after the backend has successfully generated
    /// the Old Metal Purchase transaction number.
    /// </summary>
    public bool IsPurchaseSaved =>
        !string.IsNullOrWhiteSpace(PurchaseNumber);


    /// <summary>
    /// Controls editing of the purchase.
    ///
    /// Once saved, the purchase becomes read-only until
    /// Print/Reset starts a new transaction.
    /// </summary>
    public bool CanEditPurchase =>
        !IsPurchaseSaved &&
        !IsBusy;


    // ============================================================
    // ITEM ENTRY
    // ============================================================

    [ObservableProperty]
    private string? productIdUI;

    [ObservableProperty]
    private ObservableCollection<string> metalList = new();

    [ObservableProperty]
    private ObservableCollection<string> stateReferencesList = new();

    [ObservableProperty]
    private ObservableCollection<OldMetalTransaction> omTransUIList = new();

    [ObservableProperty]
    private ObservableCollection<OldMetalTransaction> selectedRows = new();


    // ============================================================
    // TOTALS
    // ============================================================

    [ObservableProperty]
    private decimal totalGrossWeight;

    [ObservableProperty]
    private decimal totalStoneWeight;

    [ObservableProperty]
    private decimal totalNetWeight;

    [ObservableProperty]
    private decimal totalPurchaseAmount;


    // ============================================================
    // VALIDATION
    // ============================================================

    [ObservableProperty]
    private bool hasValidationErrors;

    [ObservableProperty]
    private ObservableCollection<string> validationErrors = new();


    // ============================================================
    // UI STATE
    // ============================================================

    [ObservableProperty]
    private bool isBusy;

    [ObservableProperty]
    private string? statusMessage;


    // ============================================================
    // CONSTRUCTOR
    // ============================================================

    public OldMetalPurchaseViewModel(
        ICustomerLookupService customerLookupService,
        IProductViewService productViewService,
        IOldMetalTransactionService oldMetalTransactionService,
        IOrgThisCompanyViewService orgThisCompanyViewService,
        ReferenceLoader referenceLoader,
        SettingsPageViewModel settingsPageViewModel,
        IDialogService dialogService,
        IMessageBoxService messageBoxService,
        [FromKeyedServices("ReportDialogService")]
        IDialogService reportDialogService)
    {
        _customerLookupService =
            customerLookupService;

        _productViewService =
            productViewService;

        _oldMetalTransactionService =
            oldMetalTransactionService;

        _orgThisCompanyViewService =
            orgThisCompanyViewService;

        _referenceLoader =
            referenceLoader;

        _settingsPageViewModel =
            settingsPageViewModel;

        _dialogService =
            dialogService;

        _messageBoxService =
            messageBoxService;

        _reportDialogService =
            reportDialogService;


        /*
         * SelectedRows is bound to GridControl.SelectedItems.
         *
         * Whenever the selection changes, refresh the
         * Remove command.
         */
        SelectedRows.CollectionChanged +=
            SelectedRows_CollectionChanged;


        ResetInternalState();
    }


    // ============================================================
    // GENERATED PROPERTY CHANGE HANDLERS
    // ============================================================

    partial void OnPurchaseNumberChanged(
        string? value)
    {
        OnPropertyChanged(
            nameof(IsPurchaseSaved));

        OnPropertyChanged(
            nameof(CanEditPurchase));

        RefreshCommandStates();
    }


    partial void OnIsBusyChanged(
        bool value)
    {
        OnPropertyChanged(
            nameof(CanEditPurchase));

        RefreshCommandStates();
    }


    partial void OnSelectedRowsChanged(
        ObservableCollection<OldMetalTransaction> value)
    {
        /*
         * Normally SelectedRows itself is never replaced,
         * but this makes the property safe if it is.
         */

        if (value is not null)
        {
            value.CollectionChanged -=
                SelectedRows_CollectionChanged;

            value.CollectionChanged +=
                SelectedRows_CollectionChanged;
        }

        DeleteSelectedRowsCommand
            .NotifyCanExecuteChanged();
    }


    private void SelectedRows_CollectionChanged(
        object? sender,
        NotifyCollectionChangedEventArgs e)
    {
        DeleteSelectedRowsCommand
            .NotifyCanExecuteChanged();
    }


    // ============================================================
    // COMMAND STATE
    // ============================================================

    private void RefreshCommandStates()
    {
        FetchProductCommand
            .NotifyCanExecuteChanged();

        DeleteSelectedRowsCommand
            .NotifyCanExecuteChanged();

        SaveCommand
            .NotifyCanExecuteChanged();

        PrintPreviewCommand
            .NotifyCanExecuteChanged();

        PrintCommand
            .NotifyCanExecuteChanged();
    }


    // ============================================================
    // INITIALIZATION
    // ============================================================

    [RelayCommand]
    private async Task InitializeAsync()
    {
        if (_initialized)
            return;

        _initialized = true;

        IsBusy = true;

        try
        {
            await LoadCompanyAsync();

            await LoadReferencesAsync();

            await LoadMetalListAsync();

            SetMetalPrice();

            ResetInternalState();
        }
        catch (Exception ex)
        {
            _initialized = false;

            AddValidationError(
                "Unable to initialize Old Metal Purchase: " +
                ex.Message);
        }
        finally
        {
            IsBusy = false;
        }
    }


    private async Task LoadCompanyAsync()
    {
        Company =
            await _orgThisCompanyViewService
                .GetOrgThisCompany();

        if (Company is null)
        {
            throw new InvalidOperationException(
                "Company information could not be loaded.");
        }
    }


    private async Task LoadReferencesAsync()
    {
        StateReferencesList =
            await _referenceLoader
                .LoadValuesAsync("CUST_STATE");
    }


    private async Task LoadMetalListAsync()
    {
        /*
         * Keep the same reference source currently used
         * by Old Metal Purchase.
         *
         * ProductIdUI is subsequently resolved through
         * ProductViewService.
         */

        var list =
            await _referenceLoader
                .LoadValuesAsync("OLD_METALS");

        MetalList =
            new ObservableCollection<string>(
                list);
    }


    private void SetMetalPrice()
    {
        var metalPrice =
            _settingsPageViewModel
                .GetPrice("GOLD");

        _todaysRate =
            metalPrice.GetValueOrDefault();

        OnPropertyChanged(
            nameof(TodaysRate));

        if (_todaysRate <= 0M)
        {
            AddValidationError(
                "Today's gold rate has not been entered.");
        }
    }


    // ============================================================
    // CUSTOMER LOOKUP
    // ============================================================

    [RelayCommand]
    private async Task FetchCustomerAsync(
        EditValueChangedEventArgs args)
    {
        /*
         * Do not allow customer changes after save.
         */
        if (IsPurchaseSaved)
            return;


        if (args.NewValue is not string phoneNumber)
            return;


        phoneNumber =
            phoneNumber.Trim();


        ClearCustomerValidationErrors();


        if (string.IsNullOrWhiteSpace(phoneNumber) ||
            phoneNumber.Length < 10)
        {
            return;
        }


        /*
         * Already resolved.
         */
        if (Buyer is not null &&
            Buyer.MobileNbr == phoneNumber &&
            Buyer.GKey > 0)
        {
            return;
        }


        try
        {
            CustomerReadOnly =
                false;

            _createCustomer =
                false;


            Messenger.Default.Send(
                MessageType.WaitIndicator,
                WaitIndicatorVM.ShowIndicator(
                    "Fetching Customer details..."));


            var result =
                await _customerLookupService
                    .ResolveByMobileAsync(
                        phoneNumber);


            Buyer =
                result.Customer;


            Buyer ??=
                new Customer();


            Buyer.Address ??=
                new OrgAddress();


            if (result.IsExisting)
            {
                await PrepareExistingCustomerAsync();

                return;
            }


            await PrepareNewCustomerAsync(
                phoneNumber);
        }
        catch (Exception ex)
        {
            AddValidationError(
                "Unable to fetch customer: " +
                ex.Message);
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


        Buyer.Address ??=
            new OrgAddress();


        _createCustomer =
            false;


        /*
         * Existing customer is display-only.
         *
         * Old Metal Purchase must NOT automatically
         * update customer/address master.
         */
        CustomerReadOnly =
            true;


        var gstCode =
            Buyer.Address.GstStateCode;


        if (string.IsNullOrWhiteSpace(gstCode))
        {
            gstCode =
                Buyer.GstStateCode;
        }


        if (string.IsNullOrWhiteSpace(gstCode))
        {
            gstCode =
                Company?.GstCode;

            Buyer.Address.GstStateCode =
                gstCode;
        }


        if (!string.IsNullOrWhiteSpace(gstCode))
        {
            CustomerState =
                await _referenceLoader
                    .GetValueAsync(
                        "CUST_STATE",
                        gstCode);
        }


        ClearCustomerValidationErrors();


        Messenger.Default.Send(
            "ProductIdUIName",
            MessageType.FocusTextEdit);
    }


    private async Task PrepareNewCustomerAsync(
        string phoneNumber)
    {
        Buyer ??=
            new Customer();


        Buyer.Address ??=
            new OrgAddress();


        Buyer.MobileNbr =
            phoneNumber;


        _createCustomer =
            true;

        CustomerReadOnly =
            false;


        /*
         * Use company location as default,
         * exactly like Customer Order.
         */
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

            CustomerState =
                Company.State;
        }


        /*
         * Reuse common Customer Editor.
         */
        await OpenNewCustomerEditorAsync();
    }


    private async Task OpenNewCustomerEditorAsync()
    {
        if (Buyer is null)
            return;


        var savedCustomer =
            await _dialogService
                .EditCustomerAsync(
                    Buyer,
                    isNewCustomer: true);


        /*
         * User cancelled customer creation.
         */
        if (savedCustomer is null)
        {
            _createCustomer =
                false;

            CustomerReadOnly =
                false;


            AddValidationError(
                "Customer must be created before the purchase can be saved.");

            return;
        }


        if (savedCustomer.GKey <= 0)
        {
            throw new InvalidOperationException(
                "Customer was saved but no valid GKey was returned.");
        }


        Buyer =
            savedCustomer;


        Buyer.Address ??=
            new OrgAddress();


        _createCustomer =
            false;

        CustomerReadOnly =
            true;


        var gstCode =
            Buyer.Address.GstStateCode
            ?? Buyer.GstStateCode
            ?? Company?.GstCode;


        if (!string.IsNullOrWhiteSpace(gstCode))
        {
            Buyer.Address.GstStateCode =
                gstCode;

            Buyer.GstStateCode =
                gstCode;


            CustomerState =
                await _referenceLoader
                    .GetValueAsync(
                        "CUST_STATE",
                        gstCode);
        }


        ClearCustomerValidationErrors();


        Messenger.Default.Send(
            "ProductIdUIName",
            MessageType.FocusTextEdit);
    }


    // ============================================================
    // STATE SELECTION
    // ============================================================

    partial void OnCustomerStateChanged(
        string? value)
    {
        if (CustomerReadOnly ||
            IsPurchaseSaved)
        {
            return;
        }


        _ =
            ApplyCustomerStateAsync(
                value);
    }


    private async Task ApplyCustomerStateAsync(
        string? stateName)
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
                await _referenceLoader
                    .GetCodeAsync(
                        "CUST_STATE",
                        stateName);


            Buyer.Address.State =
                stateName;


            Buyer.Address.GstStateCode =
                gstStateCode;


            /*
             * Compatibility property already used
             * elsewhere in the application.
             */
            Buyer.GstStateCode =
                gstStateCode;


            ClearCustomerValidationErrors();
        }
        catch (Exception ex)
        {
            AddValidationError(
                "Unable to resolve customer state: " +
                ex.Message);
        }
    }


    // ============================================================
    // PRODUCT / OLD METAL ITEM
    // ============================================================

    private bool CanFetchProduct()
    {
        return
            !IsPurchaseSaved &&
            !IsBusy;
    }


    [RelayCommand(
        CanExecute = nameof(CanFetchProduct))]
    private async Task FetchProductAsync()
    {
        ClearLineValidationErrors();


        if (IsPurchaseSaved)
        {
            AddValidationError(
                $"Purchase {PurchaseNumber} has already been saved and cannot be modified.");

            return;
        }


        if (string.IsNullOrWhiteSpace(ProductIdUI))
        {
            AddValidationError(
                "Select an old metal product.");

            return;
        }


        try
        {
            var product =
                await _productViewService
                    .GetProduct(
                        ProductIdUI.Trim());


            if (product is null)
            {
                AddValidationError(
                    $"Product '{ProductIdUI}' was not found.");

                return;
            }


            var metalRate =
                _settingsPageViewModel
                    .GetPrice(
                        product.Metal)
                    .GetValueOrDefault();


            if (metalRate <= 0M)
            {
                AddValidationError(
                    $"Today's rate is not available for {product.Metal}.");

                return;
            }


            var line =
                new OldMetalTransaction
                {
                    ProductGkey =
                        product.GKey,

                    ProductId =
                        product.Id,

                    ProductCategory =
                        product.Category,

                    Metal =
                        product.Metal,

                    Purity =
                        product.Purity,

                    Uom =
                        "Grams",

                    TransactedRate =
                        metalRate,

                    GrossWeight =
                        0M,

                    StoneWeight =
                        0M,

                    WastagePercent =
                        0M,

                    WastageWeight =
                        0M,

                    NetWeight =
                        0M,

                    TotalProposedPrice =
                        0M,

                    FinalPurchasePrice =
                        0M,

                    TransType =
                        "OG Purchase",

                    DocRefType =
                        "Old Purchase",

                    TransDate =
                        PurchaseDate,

                    DocRefDate =
                        PurchaseDate
                };


            OmTransUIList.Add(
                line);


            RecalculateLine(
                line);

            RecalculateTotals();


            ProductIdUI =
                string.Empty;


            Messenger.Default.Send(
                "ProductIdUIName",
                MessageType.FocusTextEdit);
        }
        catch (Exception ex)
        {
            AddValidationError(
                "Unable to add product: " +
                ex.Message);
        }
    }


    // ============================================================
    // GRID CALCULATION
    // ============================================================

    [RelayCommand]
    private void CellUpdate(
        CellValueChangedEventArgs args)
    {
        /*
         * The XAML also prevents editing after save,
         * but enforce the rule here as well.
         */
        if (IsPurchaseSaved)
            return;


        if (args.Row is not OldMetalTransaction line)
            return;


        RecalculateLine(
            line);


        RecalculateTotals();


        ClearLineValidationErrors();
    }


    private static void RecalculateLine(
        OldMetalTransaction line)
    {
        var gross =
            line.GrossWeight
                .GetValueOrDefault();


        var stone =
            line.StoneWeight
                .GetValueOrDefault();


        var wastagePercent =
            line.WastagePercent
                .GetValueOrDefault();


        /*
         * Do not allow negative values to affect
         * calculated values.
         *
         * Validation will still report invalid user input.
         */
        gross =
            Math.Max(
                gross,
                0M);

        stone =
            Math.Max(
                stone,
                0M);

        wastagePercent =
            Math.Max(
                wastagePercent,
                0M);


        /*
         * --------------------------------------------------------
         * WASTAGE
         * --------------------------------------------------------
         *
         * Wastage percentage is applied to:
         *
         *     Gross Weight - Stone Weight
         *
         * Example:
         *
         * Gross       = 10.000
         * Stone       = 1.000
         * Base Weight =  9.000
         * Wastage 2%  =  0.180
         * Net Weight  =  8.820
         *
         * If your business rule requires wastage weight
         * to remain manually entered / always zero,
         * replace this calculation with:
         *
         * line.WastageWeight ??= 0M;
         */

        var baseWeight =
            Math.Max(
                gross - stone,
                0M);


        var wastageWeight =
            baseWeight *
            wastagePercent /
            100M;


        line.WastageWeight =
            decimal.Round(
                wastageWeight,
                3,
                MidpointRounding.AwayFromZero);


        /*
         * NET WEIGHT
         */
        line.NetWeight =
            gross -
            stone -
            line.WastageWeight
                .GetValueOrDefault();


        /*
         * Never display a negative calculated net weight.
         *
         * Validation still detects stone/wastage problems.
         */
        if (line.NetWeight < 0M)
        {
            line.NetWeight =
                0M;
        }


        /*
         * PROPOSED PURCHASE PRICE
         */
        line.TotalProposedPrice =
            decimal.Round(
                line.NetWeight.GetValueOrDefault() *
                line.TransactedRate.GetValueOrDefault(),
                2,
                MidpointRounding.AwayFromZero);


        /*
         * Initialise Final Purchase Price only when
         * operator has not overridden it.
         *
         * IMPORTANT:
         * Once FinalPurchasePrice has a positive value,
         * recalculation will NOT overwrite the operator's
         * negotiated amount.
         */
        if (!line.FinalPurchasePrice.HasValue ||
            line.FinalPurchasePrice <= 0M)
        {
            line.FinalPurchasePrice =
                line.TotalProposedPrice;
        }
    }


    private void RecalculateAllLines()
    {
        foreach (var line in OmTransUIList)
        {
            RecalculateLine(
                line);
        }


        RecalculateTotals();
    }


    private void RecalculateTotals()
    {
        TotalGrossWeight =
            OmTransUIList.Sum(
                x =>
                    x.GrossWeight
                        .GetValueOrDefault());


        TotalStoneWeight =
            OmTransUIList.Sum(
                x =>
                    x.StoneWeight
                        .GetValueOrDefault());


        TotalNetWeight =
            OmTransUIList.Sum(
                x =>
                    x.NetWeight
                        .GetValueOrDefault());


        TotalPurchaseAmount =
            OmTransUIList.Sum(
                x =>
                    x.FinalPurchasePrice
                        .GetValueOrDefault());
    }


    // ============================================================
    // DELETE
    // ============================================================

    private bool CanDeleteSelectedRows()
    {
        return
            !IsPurchaseSaved &&
            !IsBusy &&
            SelectedRows.Count > 0;
    }


    [RelayCommand(
        CanExecute = nameof(CanDeleteSelectedRows))]
    private void DeleteSelectedRows()
    {
        if (IsPurchaseSaved)
        {
            AddValidationError(
                $"Purchase {PurchaseNumber} has already been saved and cannot be modified.");

            return;
        }


        if (SelectedRows.Count == 0)
            return;


        /*
         * Copy first because removing items from
         * OmTransUIList can modify grid selection.
         */
        var rowsToDelete =
            SelectedRows
                .ToList();


        foreach (var row in rowsToDelete)
        {
            OmTransUIList.Remove(
                row);
        }


        SelectedRows.Clear();


        RecalculateTotals();


        ClearLineValidationErrors();


        DeleteSelectedRowsCommand
            .NotifyCanExecuteChanged();
    }


    // ============================================================
    // OPTIONAL SINGLE ROW DELETE
    //
    // Keep this because another UI/context menu may already use
    // DeleteSingleRowCommand.
    // ============================================================

    private bool CanDeleteSingleRow(
        OldMetalTransaction? line)
    {
        return
            !IsPurchaseSaved &&
            !IsBusy &&
            line is not null &&
            OmTransUIList.Contains(line);
    }


    [RelayCommand(
        CanExecute = nameof(CanDeleteSingleRow))]
    private void DeleteSingleRow(
        OldMetalTransaction line)
    {
        if (IsPurchaseSaved)
            return;


        if (line is null)
            return;


        OmTransUIList.Remove(
            line);


        SelectedRows.Remove(
            line);


        RecalculateTotals();


        ClearLineValidationErrors();


        DeleteSelectedRowsCommand
            .NotifyCanExecuteChanged();
    }


    // ============================================================
    // VALIDATION
    // ============================================================

    private bool PrepareAndValidatePurchase()
    {
        RecalculateAllLines();

        return
            ValidatePurchase();
    }


    private bool ValidatePurchase()
    {
        ValidationErrors.Clear();


        // --------------------------------------------------------
        // SAVED STATE
        // --------------------------------------------------------

        if (IsPurchaseSaved)
        {
            ValidationErrors.Add(
                $"Purchase {PurchaseNumber} has already been saved.");

            HasValidationErrors =
                true;

            return false;
        }


        // --------------------------------------------------------
        // CUSTOMER
        // --------------------------------------------------------

        if (Buyer is null)
        {
            ValidationErrors.Add(
                "Customer details are required.");
        }
        else
        {
            if (string.IsNullOrWhiteSpace(
                    Buyer.MobileNbr))
            {
                ValidationErrors.Add(
                    "Customer mobile number is required.");
            }


            if (string.IsNullOrWhiteSpace(
                    Buyer.CustomerName))
            {
                ValidationErrors.Add(
                    "Customer name is required.");
            }


            if (Buyer.GKey <= 0)
            {
                ValidationErrors.Add(
                    "Customer must be saved before saving the purchase.");
            }


            if (Buyer.Address is null ||
                string.IsNullOrWhiteSpace(
                    Buyer.Address.State))
            {
                ValidationErrors.Add(
                    "Customer state is required.");
            }
        }


        // --------------------------------------------------------
        // RATE
        // --------------------------------------------------------

        if (_todaysRate <= 0M)
        {
            ValidationErrors.Add(
                "Today's gold rate is not available.");
        }


        // --------------------------------------------------------
        // LINES
        // --------------------------------------------------------

        if (OmTransUIList.Count == 0)
        {
            ValidationErrors.Add(
                "Add at least one old metal item.");
        }
        else
        {
            for (var index = 0;
                 index < OmTransUIList.Count;
                 index++)
            {
                var line =
                    OmTransUIList[index];


                var prefix =
                    $"Line {index + 1}";


                if (string.IsNullOrWhiteSpace(
                        line.ProductId))
                {
                    ValidationErrors.Add(
                        $"{prefix}: Product is required.");
                }


                if (line.ProductGkey.GetValueOrDefault() <= 0)
                {
                    ValidationErrors.Add(
                        $"{prefix}: Invalid product reference.");
                }


                var gross =
                    line.GrossWeight
                        .GetValueOrDefault();


                var stone =
                    line.StoneWeight
                        .GetValueOrDefault();


                var wastagePercent =
                    line.WastagePercent
                        .GetValueOrDefault();


                var wastageWeight =
                    line.WastageWeight
                        .GetValueOrDefault();


                var net =
                    line.NetWeight
                        .GetValueOrDefault();


                var rate =
                    line.TransactedRate
                        .GetValueOrDefault();


                var finalAmount =
                    line.FinalPurchasePrice
                        .GetValueOrDefault();


                if (gross <= 0M)
                {
                    ValidationErrors.Add(
                        $"{prefix}: Gross weight must be greater than zero.");
                }


                if (stone < 0M)
                {
                    ValidationErrors.Add(
                        $"{prefix}: Stone weight cannot be negative.");
                }


                if (stone > gross)
                {
                    ValidationErrors.Add(
                        $"{prefix}: Stone weight cannot exceed gross weight.");
                }


                if (wastagePercent < 0M)
                {
                    ValidationErrors.Add(
                        $"{prefix}: Wastage percentage cannot be negative.");
                }


                if (wastagePercent > 100M)
                {
                    ValidationErrors.Add(
                        $"{prefix}: Wastage percentage cannot exceed 100%.");
                }


                if (wastageWeight < 0M)
                {
                    ValidationErrors.Add(
                        $"{prefix}: Wastage weight cannot be negative.");
                }


                if (stone + wastageWeight > gross)
                {
                    ValidationErrors.Add(
                        $"{prefix}: Stone and wastage weight cannot exceed gross weight.");
                }


                if (net <= 0M)
                {
                    ValidationErrors.Add(
                        $"{prefix}: Net weight must be greater than zero.");
                }


                if (rate <= 0M)
                {
                    ValidationErrors.Add(
                        $"{prefix}: Metal rate is not available.");
                }


                if (finalAmount <= 0M)
                {
                    ValidationErrors.Add(
                        $"{prefix}: Final purchase amount must be greater than zero.");
                }
            }
        }


        HasValidationErrors =
            ValidationErrors.Count > 0;


        return
            !HasValidationErrors;
    }


    // ============================================================
    // VALIDATION HELPERS
    // ============================================================

    private void AddValidationError(
        string message)
    {
        if (!ValidationErrors.Contains(
                message))
        {
            ValidationErrors.Add(
                message);
        }


        HasValidationErrors =
            ValidationErrors.Count > 0;
    }


    private void ClearValidationErrors()
    {
        ValidationErrors.Clear();

        HasValidationErrors =
            false;
    }


    private void ClearCustomerValidationErrors()
    {
        RemoveValidationErrorsContaining(
            "Customer");

        RemoveValidationErrorsContaining(
            "customer");

        RemoveValidationErrorsContaining(
            "mobile");

        RemoveValidationErrorsContaining(
            "state");
    }


    private void ClearLineValidationErrors()
    {
        var items =
            ValidationErrors
                .Where(
                    x =>
                        x.StartsWith(
                            "Line ",
                            StringComparison.OrdinalIgnoreCase) ||

                        x.Contains(
                            "old metal item",
                            StringComparison.OrdinalIgnoreCase) ||

                        x.Contains(
                            "product",
                            StringComparison.OrdinalIgnoreCase))
                .ToList();


        foreach (var item in items)
        {
            ValidationErrors.Remove(
                item);
        }


        HasValidationErrors =
            ValidationErrors.Count > 0;
    }


    private void RemoveValidationErrorsContaining(
        string text)
    {
        var items =
            ValidationErrors
                .Where(
                    x =>
                        x.Contains(
                            text,
                            StringComparison.OrdinalIgnoreCase))
                .ToList();


        foreach (var item in items)
        {
            ValidationErrors.Remove(
                item);
        }


        HasValidationErrors =
            ValidationErrors.Count > 0;
    }


    // ============================================================
    // CUSTOMER MUST ALREADY BE SAVED
    // ============================================================

    private Task EnsureCustomerSavedAsync()
    {
        if (Buyer is null)
        {
            throw new InvalidOperationException(
                "Customer information is missing.");
        }


        /*
         * New customer is created through the common
         * Customer Editor.
         *
         * Old Metal Purchase never directly creates or
         * updates Customer / OrgAddress.
         */
        if (Buyer.GKey <= 0)
        {
            throw new InvalidOperationException(
                "Please create/save the customer before saving the purchase.");
        }


        return
            Task.CompletedTask;
    }


    // ============================================================
    // SAVE
    // ============================================================

    private bool CanSave()
    {
        return
            !IsBusy &&
            !IsPurchaseSaved;
    }


    [RelayCommand(
        CanExecute = nameof(CanSave))]
    private async Task SaveAsync()
    {
        if (IsBusy)
            return;


        /*
         * Critical double-save protection.
         */
        if (IsPurchaseSaved)
        {
            AddValidationError(
                $"Purchase {PurchaseNumber} has already been saved.");

            return;
        }


        try
        {
            // ----------------------------------------------------
            // 1. CALCULATE + VALIDATE
            // ----------------------------------------------------

            if (!PrepareAndValidatePurchase())
                return;


            // ----------------------------------------------------
            // 2. CUSTOMER MUST ALREADY EXIST
            // ----------------------------------------------------

            await EnsureCustomerSavedAsync();


            // ----------------------------------------------------
            // 3. PREPARE TRANSACTION LINES
            // ----------------------------------------------------

            PrepareLinesForSave();


            // ----------------------------------------------------
            // 4. SAVE BATCH
            // ----------------------------------------------------

            IsBusy =
                true;


            StatusMessage =
                "Saving old metal purchase...";


            Messenger.Default.Send(
                MessageType.WaitIndicator,
                WaitIndicatorVM.ShowIndicator(
                    "Saving Old Metal Purchase..."));


            var transactionNumber =
                await _oldMetalTransactionService
                    .CreateOldMetalTransaction(
                        OmTransUIList);


            if (string.IsNullOrWhiteSpace(
                    transactionNumber))
            {
                throw new InvalidOperationException(
                    "Old Metal Purchase save returned no transaction number.");
            }


            // ----------------------------------------------------
            // 5. STORE RETURNED DOCUMENT NUMBER
            // ----------------------------------------------------

            PurchaseNumber =
                transactionNumber;


            StatusMessage =
                $"Old Metal Purchase {PurchaseNumber} saved successfully.";


            ClearValidationErrors();


            RefreshCommandStates();


            _messageBoxService.ShowMessage(
                $"Old Metal Purchase {PurchaseNumber} saved successfully.",
                "Old Metal Purchase",
                MessageButton.OK,
                MessageIcon.Information);
        }
        catch (Exception ex)
        {
            AddValidationError(
                "Failed to save Old Metal Purchase: " +
                ex.Message);
        }
        finally
        {
            IsBusy =
                false;


            Messenger.Default.Send(
                MessageType.WaitIndicator,
                WaitIndicatorVM.HideIndicator());


            RefreshCommandStates();
        }
    }


    private void PrepareLinesForSave()
    {
        if (Buyer is null)
            return;


        foreach (var line in OmTransUIList)
        {
            line.CustGkey =
                Buyer.GKey;


            line.CustMobile =
                Buyer.MobileNbr;


            line.TransType =
                "OG Purchase";


            line.DocRefType =
                "Old Purchase";


            line.TransDate =
                PurchaseDate;


            line.DocRefDate =
                PurchaseDate;


            if (string.IsNullOrWhiteSpace(
                    line.Uom))
            {
                line.Uom =
                    "Grams";
            }
        }
    }


    // ============================================================
    // PRINT
    // ============================================================

    private bool CanPrint()
    {
        return
            !IsBusy &&
            IsPurchaseSaved;
    }


    [RelayCommand(
        CanExecute = nameof(CanPrint))]
    private void PrintPreview()
    {
        if (!CanPrint())
            return;


        try
        {
            _reportDialogService
                .PrintPreviewOMPurchase(
                    PurchaseNumber);
        }
        catch (Exception ex)
        {
            AddValidationError(
                $"Unable to preview Old Metal Purchase: {ex.Message}");
        }
    }


    [RelayCommand(
        CanExecute = nameof(CanPrint))]
    private void Print()
    {
        if (!CanPrint())
            return;


        try
        {
            /*
             * IMPORTANT:
             *
             * Your uploaded ViewModel currently calls
             * PrintPreviewOMPurchase() here.
             *
             * Replace the following method name with your
             * existing direct-print extension if its exact
             * name differs.
             */
            _reportDialogService
                .PrintPreviewOMPurchase(
                    PurchaseNumber);


            /*
             * Only reset after the print call succeeds.
             */
            ResetInternalState();


            Messenger.Default.Send(
                "CustomerMobileNbr",
                MessageType.FocusTextEdit);
        }
        catch (Exception ex)
        {
            /*
             * Do NOT reset when printing throws.
             * The saved purchase remains available so the
             * operator can retry.
             */
            AddValidationError(
                $"Unable to print Old Metal Purchase: {ex.Message}");
        }
    }


    // ============================================================
    // RESET
    // ============================================================

    [RelayCommand]
    private void Reset()
    {
        ResetInternalState();


        Messenger.Default.Send(
            "CustomerMobileNbr",
            MessageType.FocusTextEdit);
    }


    private void ResetInternalState()
    {
        CustomerPhoneNumber =
            string.Empty;


        Buyer =
            new Customer
            {
                Address =
                    new OrgAddress()
            };


        CustomerState =
            Company?.State;


        CustomerReadOnly =
            false;


        _createCustomer =
            false;


        PurchaseNumber =
            string.Empty;


        PurchaseDate =
            DateTime.Now;


        ProductIdUI =
            string.Empty;


        OmTransUIList.Clear();

        SelectedRows.Clear();


        TotalGrossWeight =
            0M;


        TotalStoneWeight =
            0M;


        TotalNetWeight =
            0M;


        TotalPurchaseAmount =
            0M;


        StatusMessage =
            string.Empty;


        ClearValidationErrors();


        /*
         * Do NOT reload Company or References here.
         *
         * Reset must remain a local operation and must
         * never call the server.
         */


        OnPropertyChanged(
            nameof(IsPurchaseSaved));

        OnPropertyChanged(
            nameof(CanEditPurchase));


        RefreshCommandStates();
    }
}