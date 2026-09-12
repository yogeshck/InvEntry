using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DevExpress.Mvvm;
using DevExpress.Xpf.Core;
using InvEntry.Contracts.StockTransfers;
using InvEntry.Extension;
using InvEntry.Models;
using InvEntry.Services;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using System.Threading.Tasks;

namespace InvEntry.ViewModels;

public partial class OldMetalTransferEntryViewModel : ObservableObject
{
    private const string TransferTypeOldMetal = "OLD_METAL";


    // =========================================================
    // HEADER / UI
    // =========================================================

    [ObservableProperty]
    private OrgThisCompanyView? _company;

    [ObservableProperty]
    private string? _fromBranch;

    [ObservableProperty]
    private string? _sentTo;

    [ObservableProperty]
    private DateTime _transferDate = DateTime.Now;

    [ObservableProperty]
    private string? _transferNbr;

    [ObservableProperty]
    private int _transferGkey;

    [ObservableProperty]
    private string? _transferRemarks;


    // =========================================================
    // LINE ENTRY
    //
    // Old Metal Transfer is a NET-WEIGHT movement.
    //
    // We do not ask the operator for:
    //      Gross Weight
    //      Stone Weight
    //      Rate
    //      Value
    //
    // TransferNetWeight is the actual old-metal weight
    // being moved out for melting.
    // =========================================================

    [ObservableProperty]
    private string? _oldMetalIdUI;

    [ObservableProperty]
    private decimal _transferNetWeight;

    [ObservableProperty]
    private string? _oMTransDesc;


    // =========================================================
    // SELECTED PRODUCT STOCK
    //
    // SelectedCurrentStock:
    //      PRODUCT_STOCK_SUMMARY.BALANCE_WEIGHT
    //
    // SelectedAvailableStock:
    //      Current Stock less any weight already staged
    //      in this unsaved transfer.
    // =========================================================

    [ObservableProperty]
    private decimal _selectedCurrentStock;

    [ObservableProperty]
    private decimal _selectedAvailableStock;


    // =========================================================
    // LOOKUPS
    // =========================================================

    [ObservableProperty]
    private ObservableCollection<string> _oldMetalList = new();

    [ObservableProperty]
    private ObservableCollection<string> _receipientStrList = new();

    [ObservableProperty]
    private ObservableCollection<MtblReference> _receipientsList = new();


    // =========================================================
    // MULTI-LINE GRID
    //
    // Do not bind CreateStockTransferLineRequest directly
    // to the UI anymore.
    //
    // CurrentStock and BalanceAfterTransfer are UI values
    // and should not become part of the API contract.
    // =========================================================

    [ObservableProperty]
    private ObservableCollection<OldMetalTransferLineItem>
        _omTransUIList = new();

    [ObservableProperty]
    private ObservableCollection<OldMetalTransferLineItem>
        _selectedRows = new();


    // =========================================================
    // STATE
    // =========================================================

    [ObservableProperty]
    private bool _canEditPurchase = true;

    [ObservableProperty]
    private bool _isBusy;

    [ObservableProperty]
    private string? _statusMessage;


    // =========================================================
    // SERVICES
    // =========================================================

    private readonly IStockTransferService _stockTransferService;

    private readonly IOrgThisCompanyViewService
        _orgThisCompanyViewService;

    private readonly IMtblReferencesService
        _mtblReferencesService;

    private readonly IProductViewService
        _productViewService;

    private readonly IProductStockSummaryService
        _productStockSummaryService;

    private readonly IMessageBoxService
        _messageBoxService;


    // =========================================================
    // CONSTRUCTOR
    // =========================================================

    public OldMetalTransferEntryViewModel(
        IStockTransferService stockTransferService,
        IOrgThisCompanyViewService orgThisCompanyViewService,
        IMtblReferencesService mtblReferencesService,
        IProductViewService productViewService,
        IProductStockSummaryService productStockSummaryService,
        IMessageBoxService messageBoxService)
    {
        _stockTransferService =
            stockTransferService;

        _orgThisCompanyViewService =
            orgThisCompanyViewService;

        _mtblReferencesService =
            mtblReferencesService;

        _productViewService =
            productViewService;

        _productStockSummaryService =
            productStockSummaryService;

        _messageBoxService =
            messageBoxService;


        OmTransUIList.CollectionChanged +=
            OmTransUIList_CollectionChanged;


        Initialize();
    }


    // =========================================================
    // INITIALIZE
    // =========================================================

    private async void Initialize()
    {
        try
        {
            IsBusy = true;


            await SetThisCompanyAsync();

            await PopulateReceipientListAsync();

            await PopulateOldMetalListAsync();


            ResetTransfer();
        }
        catch (Exception ex)
        {
            _messageBoxService.ShowMessage(
                ex.Message,
                "Old Metal Transfer",
                MessageButton.OK,
                MessageIcon.Error);
        }
        finally
        {
            IsBusy = false;
        }
    }


    // =========================================================
    // COMPANY
    // =========================================================

    private async Task SetThisCompanyAsync()
    {
        Company =
            await _orgThisCompanyViewService
                .GetOrgThisCompany();


        FromBranch =
            Company?.CompanyName;
    }


    // =========================================================
    // DESTINATION LOOKUP
    // =========================================================

    private async Task PopulateReceipientListAsync()
    {
        var references =
            await _mtblReferencesService
                .GetReferenceList(
                    "STOCK_TRANSFER");


        var activeList =
            references?
                .Where(x => x.IsActive)
                .OrderBy(x => x.SortSeq)
                .ToList()
            ?? new List<MtblReference>();


        ReceipientsList =
            new ObservableCollection<MtblReference>(
                activeList);


        ReceipientStrList =
            new ObservableCollection<string>(
                activeList.Select(
                    x => x.RefCode));
    }


    // =========================================================
    // OLD METAL LOOKUP
    // =========================================================

    private async Task PopulateOldMetalListAsync()
    {
        var metalRefList =
            await _mtblReferencesService
                .GetReferenceList(
                    "OLD_METALS");


        OldMetalList =
            new ObservableCollection<string>(
                metalRefList
                    .Where(x => x.IsActive)
                    .OrderBy(x => x.SortSeq)
                    .Select(x => x.RefValue));
    }


    // =========================================================
    // DESTINATION CHANGED
    // =========================================================

    partial void OnSentToChanged(
        string? value)
    {
        CreateStockTransferCommand
            .NotifyCanExecuteChanged();
    }


    // =========================================================
    // OLD METAL PRODUCT CHANGED
    //
    // As soon as the operator selects an old-metal product,
    // obtain its current stock from PRODUCT_STOCK_SUMMARY.
    //
    // IMPORTANT:
    //
    // PRODUCT_STOCK_SUMMARY.PRODUCT_GKEY must be populated.
    // CATEGORY is not used as the stock relationship.
    // =========================================================

    partial void OnOldMetalIdUIChanged(
        string? value)
    {
        SelectedCurrentStock = 0M;

        SelectedAvailableStock = 0M;


        if (string.IsNullOrWhiteSpace(value))
        {
            return;
        }


        _ = LoadSelectedProductStockAsync(
            value);
    }


    // =========================================================
    // LOAD SELECTED PRODUCT STOCK
    // =========================================================

    private async Task LoadSelectedProductStockAsync(
        string productId)
    {
        try
        {
            // -----------------------------------------------------
            // Resolve product master first.
            // -----------------------------------------------------

            var product =
                await _productViewService
                    .GetProduct(
                        productId);


            if (product is null)
            {
                SelectedCurrentStock = 0M;
                SelectedAvailableStock = 0M;

                return;
            }


            // -----------------------------------------------------
            // Read authoritative stock summary.
            //
            // Existing API:
            //
            // GET
            // api/ProductStockSummary/productGkey/{productGkey}
            // -----------------------------------------------------

            var stock =
                await _productStockSummaryService
                    .GetByProductGkey(
                        product.GKey);


            var currentStock =
                stock?.BalanceWeight ?? 0M;


            // -----------------------------------------------------
            // Deduct any same product already staged in the
            // current unsaved transfer.
            // -----------------------------------------------------

            var alreadyStaged =
                OmTransUIList
                    .Where(
                        x =>
                            x.ProductGkey ==
                            product.GKey)
                    .Sum(
                        x =>
                            x.TransferWeight);


            SelectedCurrentStock =
                currentStock;


            SelectedAvailableStock =
                Math.Max(
                    0M,
                    currentStock -
                    alreadyStaged);
        }
        catch
        {
            /*
             * This method runs automatically while product
             * selection changes.
             *
             * Do not interrupt the operator with a popup while
             * ComboBox text is still changing.
             *
             * FetchProduct() performs the authoritative check
             * again when Add Item is pressed.
             */

            SelectedCurrentStock = 0M;

            SelectedAvailableStock = 0M;
        }
    }


    // =========================================================
    // ADD ITEM
    //
    // One Add Item = one old-metal product line.
    //
    // The entered weight is NET old-metal weight.
    // =========================================================

    [RelayCommand]
    private async Task FetchProduct()
    {
        if (!CanEditPurchase ||
            IsBusy)
        {
            return;
        }


        // ---------------------------------------------------------
        // PRODUCT
        // ---------------------------------------------------------

        if (string.IsNullOrWhiteSpace(
                OldMetalIdUI))
        {
            _messageBoxService.ShowMessage(
                "Please select the old metal product.",
                "Old Metal Transfer",
                MessageButton.OK,
                MessageIcon.Warning);

            return;
        }


        // ---------------------------------------------------------
        // TRANSFER NET WEIGHT
        // ---------------------------------------------------------

        if (TransferNetWeight <= 0M)
        {
            _messageBoxService.ShowMessage(
                "Transfer weight must be greater than zero.",
                "Invalid Weight",
                MessageButton.OK,
                MessageIcon.Warning);

            return;
        }


        try
        {
            IsBusy = true;


            StatusMessage =
                "Checking old metal stock...";


            // =====================================================
            // 1. PRODUCT MASTER
            // =====================================================

            var product =
                await _productViewService
                    .GetProduct(
                        OldMetalIdUI);


            if (product is null)
            {
                _messageBoxService.ShowMessage(
                    $"Old metal product '{OldMetalIdUI}' " +
                    $"was not found.",
                    "Product Not Found",
                    MessageButton.OK,
                    MessageIcon.Warning);

                return;
            }


            // =====================================================
            // 2. PREVENT DUPLICATE PRODUCT
            //
            // For the first release, keep one row per ProductGkey.
            //
            // If operator wants to change weight, remove the
            // existing row and add it again.
            // =====================================================

            var existingLine =
                OmTransUIList
                    .FirstOrDefault(
                        x =>
                            x.ProductGkey ==
                            product.GKey);


            if (existingLine is not null)
            {
                _messageBoxService.ShowMessage(
                    $"{product.Id} is already included " +
                    $"in this transfer.\n\n" +
                    $"Remove the existing line and add it again " +
                    $"if the transfer weight needs to be changed.",
                    "Product Already Added",
                    MessageButton.OK,
                    MessageIcon.Warning);

                return;
            }


            // =====================================================
            // 3. GET CURRENT STOCK
            //
            // PRODUCT_STOCK_SUMMARY.BALANCE_WEIGHT
            // =====================================================

            var stock =
                await _productStockSummaryService
                    .GetByProductGkey(
                        product.GKey);


            if (stock is null)
            {
                _messageBoxService.ShowMessage(
                    $"Stock summary was not found for " +
                    $"{product.Id}.\n\n" +
                    $"Please verify PRODUCT_STOCK_SUMMARY.",
                    "Stock Not Found",
                    MessageButton.OK,
                    MessageIcon.Warning);

                return;
            }


            var currentStock =
                 stock.BalanceWeight ?? 0M;


            SelectedCurrentStock =
                currentStock;


            // =====================================================
            // 4. CALCULATE ALREADY STAGED WEIGHT
            //
            // With duplicate prevention this should normally be
            // zero, but keeping this calculation makes the logic
            // safe if duplicate rows are allowed later.
            // =====================================================

            var alreadyStaged =
                OmTransUIList
                    .Where(
                        x =>
                            x.ProductGkey ==
                            product.GKey)
                    .Sum(
                        x =>
                            x.TransferWeight);


            var availableStock =
                currentStock -
                alreadyStaged;


            SelectedAvailableStock =
                Math.Max(
                    0M,
                    availableStock);


            // =====================================================
            // 5. NO STOCK
            // =====================================================

            if (availableStock <= 0M)
            {
                _messageBoxService.ShowMessage(
                    $"No old metal stock is available for " +
                    $"{product.Id}.\n\n" +
                    $"Current stock: {currentStock:N3} g",
                    "Insufficient Stock",
                    MessageButton.OK,
                    MessageIcon.Warning);

                //return;
            }


            // =====================================================
            // 6. TRANSFER CANNOT EXCEED STOCK
            // =====================================================

/*            if (TransferNetWeight >
                availableStock)
            {
                _messageBoxService.ShowMessage(
                    $"Available stock : {availableStock:N3} g\n" +
                    $"Transfer weight : {TransferNetWeight:N3} g\n\n" +
                    $"Transfer weight cannot exceed " +
                    $"available stock.",
                    "Insufficient Stock",
                    MessageButton.OK,
                    MessageIcon.Warning);

                //return;
            }*/


            // =====================================================
            // 7. CREATE UI GRID LINE
            // =====================================================

            var line =
                new OldMetalTransferLineItem
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
                        stock.Uom ?? "Grams",

                    CurrentStock =
                        currentStock,

                    TransferWeight =
                        TransferNetWeight,

                    BalanceAfterTransfer =
                        currentStock -
                        TransferNetWeight,

                    Notes =
                        string.IsNullOrWhiteSpace(
                            OMTransDesc)
                            ? null
                            : OMTransDesc.Trim()
                };


            // =====================================================
            // 8. ADD TO TEMPORARY DOCUMENT
            // =====================================================

            OmTransUIList.Add(
                line);


            StatusMessage =
                $"{product.Id}: " +
                $"{TransferNetWeight:N3} g added. " +
                $"Balance {line.BalanceAfterTransfer:N3} g.";


            // =====================================================
            // 9. CLEAR CURRENT ENTRY
            // =====================================================

            ClearLineEntry();


            //CreateStockTransferCommand
            //    .NotifyCanExecuteChanged();
        }
        catch (Exception ex)
        {
            _messageBoxService.ShowMessage(
                ex.Message,
                "Add Old Metal Item",
                MessageButton.OK,
                MessageIcon.Error);
        }
        finally
        {
            IsBusy = false;
        }
    }


    // =========================================================
    // CLEAR CURRENT ENTRY
    // =========================================================

    private void ClearLineEntry()
    {
        OldMetalIdUI = null;

        TransferNetWeight = 0M;

        OMTransDesc = null;

        SelectedCurrentStock = 0M;

        SelectedAvailableStock = 0M;


        Messenger.Default.Send(
            "ProductIdUIName",
            MessageType.FocusTextEdit);
    }


    // =========================================================
    // DELETE SELECTED ROWS
    // =========================================================

    [RelayCommand]
    private void DeleteSelectedRows()
    {
        if (!CanEditPurchase)
        {
            return;
        }


        if (SelectedRows is null ||
            SelectedRows.Count == 0)
        {
            return;
        }


        var rows =
            SelectedRows.ToList();


        foreach (var row in rows)
        {
            OmTransUIList.Remove(
                row);
        }


        SelectedRows.Clear();


        StatusMessage =
            $"{OmTransUIList.Count} item(s) remaining.";


        CreateStockTransferCommand
            .NotifyCanExecuteChanged();
    }


    // =========================================================
    // SAVE
    // =========================================================

    [RelayCommand(
        CanExecute = nameof(
            CanCreateStockTransfer))]
    private async Task CreateStockTransfer()
    {
        if (!CanCreateStockTransfer())
        {
            return;
        }


        // ---------------------------------------------------------
        // DESTINATION
        // ---------------------------------------------------------

        var destination =
            ReceipientsList
                .FirstOrDefault(
                    x =>
                        string.Equals(
                            x.RefCode,
                            SentTo,
                            StringComparison.OrdinalIgnoreCase));


        if (destination is null)
        {
            _messageBoxService.ShowMessage(
                "Please select a valid destination branch.",
                "Destination Required",
                MessageButton.OK,
                MessageIcon.Warning);

            return;
        }


        // ---------------------------------------------------------
        // SOURCE COMPANY
        // ---------------------------------------------------------

        if (Company is null)
        {
            _messageBoxService.ShowMessage(
                "Source company details are unavailable.",
                "Company Details",
                MessageButton.OK,
                MessageIcon.Error);

            return;
        }


        // ---------------------------------------------------------
        // ITEMS
        // ---------------------------------------------------------

        if (OmTransUIList.Count == 0)
        {
            _messageBoxService.ShowMessage(
                "Please add at least one old metal item.",
                "No Items",
                MessageButton.OK,
                MessageIcon.Warning);

            return;
        }


        try
        {
            IsBusy = true;


            StatusMessage =
                "Saving old metal transfer...";


            // =====================================================
            // IMPORTANT
            //
            // Re-check stock immediately before POST.
            //
            // This improves the WPF user experience.
            //
            // The API must STILL perform its own authoritative
            // stock validation inside its transaction.
            // =====================================================

            foreach (var line in OmTransUIList)
            {
                var stock =
                    await _productStockSummaryService
                        .GetByProductGkey(
                            line.ProductGkey);


                if (stock is null)
                {
                    _messageBoxService.ShowMessage(
                        $"Stock summary was not found for " +
                        $"{line.ProductId}.",
                        "Stock Not Found",
                        MessageButton.OK,
                        MessageIcon.Warning);

                    return;
                }


                var latestStock =
                    stock.BalanceWeight;


                if (line.TransferWeight >
                    latestStock)
                {
                    _messageBoxService.ShowMessage(
                        $"Stock has changed for {line.ProductId}.\n\n" +
                        $"Available stock : {latestStock:N3} g\n" +
                        $"Transfer weight : {line.TransferWeight:N3} g\n\n" +
                        $"Please remove the line and add it again.",
                        "Stock Changed",
                        MessageButton.OK,
                        MessageIcon.Warning);

                    return;
                }
            }


            // =====================================================
            // CREATE REQUEST
            // =====================================================

            var request =
                new CreateStockTransferRequest
                {
                    TransferDate =
                        TransferDate,

                    TransferType =
                        TransferTypeOldMetal,

                    FromBranch =
                        Company.CompanyName,

                    FromTenantGkey =
                        Company.TenantGkey,

                    /*
                     * Existing MtblReference FK.
                     */
                    ToReferenceGkey =
                        destination.GKey,

                    Remarks =
                        string.IsNullOrWhiteSpace(
                            TransferRemarks)
                            ? null
                            : TransferRemarks.Trim(),

                    Lines =
                        OmTransUIList
                            .Select(
                                MapToRequestLine)
                            .ToList()
                };


            // =====================================================
            // POST ONE MULTI-LINE TRANSFER
            // =====================================================

            var saved =
                await _stockTransferService
                    .CreateAsync(
                        request);


            if (saved is null)
            {
                _messageBoxService.ShowMessage(
                    "The transfer could not be created.",
                    "Old Metal Transfer",
                    MessageButton.OK,
                    MessageIcon.Error);

                return;
            }


            TransferGkey =
                saved.Gkey;


            TransferNbr =
                saved.TransferNbr;


            CanEditPurchase =
                false;


            StatusMessage =
                $"Transfer {TransferNbr} created successfully.";


            CreateStockTransferCommand
                .NotifyCanExecuteChanged();

            PrintPreviewStockTransferCommand
                .NotifyCanExecuteChanged();

            PrintStockTransferCommand
                .NotifyCanExecuteChanged();


            _messageBoxService.ShowMessage(
                $"Old Metal Transfer {TransferNbr} " +
                $"created successfully.",
                "Transfer Created",
                MessageButton.OK,
                MessageIcon.Information);
        }
        catch (Exception ex)
        {
            _messageBoxService.ShowMessage(
                ex.Message,
                "Old Metal Transfer",
                MessageButton.OK,
                MessageIcon.Error);
        }
        finally
        {
            IsBusy = false;
        }
    }


    // =========================================================
    // MAP UI LINE TO EXISTING STOCK TRANSFER CONTRACT
    //
    // Business rule:
    //
    // Old Metal Transfer is NET-WEIGHT movement.
    //
    // Existing API still contains:
    //
    //      GrossWeight
    //      StoneWeight
    //      NetWeight
    //
    // Therefore:
    //
    //      Gross = Transfer Weight
    //      Stone = 0
    //      Net   = Transfer Weight
    //
    // Rate/value are not required for melting issue.
    // =========================================================

    private static CreateStockTransferLineRequest
        MapToRequestLine(
            OldMetalTransferLineItem source)
    {
        return new CreateStockTransferLineRequest
        {
            ProductStockGkey =
                null,

            ProductGkey =
                source.ProductGkey,

            ProductId =
                source.ProductId,

            Metal =
                source.Metal,

            Purity =
                source.Purity,

            Uom =
                source.Uom,

            Qty =
                1,

            GrossWeight =
                source.TransferWeight,

            StoneWeight =
                0M,

            NetWeight =
                source.TransferWeight,

            TransactedRate =
                null,

            TransferValue =
                null,

            Notes =
                source.Notes
        };
    }


    // =========================================================
    // CAN SAVE
    // =========================================================

    private bool CanCreateStockTransfer()
    {
        return
            CanEditPurchase &&
            !IsBusy &&
            string.IsNullOrWhiteSpace(
                TransferNbr) &&
            !string.IsNullOrWhiteSpace(
                SentTo) &&
            OmTransUIList.Count > 0;
    }


    // =========================================================
    // RESET
    // =========================================================

    [RelayCommand]
    private void ResetOldMetalTrans()
    {
        TransferNbr = null;

        TransferGkey = 0;

        TransferDate = DateTime.Now;

        TransferRemarks = null;

        SentTo = null;

        CanEditPurchase = true;

        StatusMessage = null;


        OmTransUIList.Clear();

        SelectedRows.Clear();

        ClearLineEntry();


        CreateStockTransferCommand
            .NotifyCanExecuteChanged();

        PrintPreviewStockTransferCommand
            .NotifyCanExecuteChanged();

        PrintStockTransferCommand
            .NotifyCanExecuteChanged();
    }


    private void ResetTransfer()
    {
        TransferNbr = null;

        TransferGkey = 0;

        TransferDate = DateTime.Now;

        TransferRemarks = null;

        SentTo = null;

        CanEditPurchase = true;

        StatusMessage = null;


        OmTransUIList.Clear();

        SelectedRows.Clear();

        ClearLineEntry();
    }


    // =========================================================
    // PRINT / PREVIEW
    //
    // Existing Delivery Note report is Estimate-backed.
    //
    // This screen now saves STOCK_TRANSFER_HEADER /
    // STOCK_TRANSFER_LINE directly.
    //
    // Keep printing disabled functionally until the dedicated
    // Stock Transfer report is wired.
    // =========================================================

    [RelayCommand(
        CanExecute = nameof(
            CanPrintStockTransfer))]
    private void PrintPreviewStockTransfer()
    {
        _messageBoxService.ShowMessage(
            "The transfer was saved successfully.\n\n" +
            "The existing Delivery Note report is still linked " +
            "to the legacy Estimate document. A dedicated Stock " +
            "Transfer report must be connected before Preview " +
            "is enabled.",
            "Print Preview",
            MessageButton.OK,
            MessageIcon.Information);
    }


    [RelayCommand(
        CanExecute = nameof(
            CanPrintStockTransfer))]
    private void PrintStockTransfer()
    {
        _messageBoxService.ShowMessage(
            "The transfer was saved successfully.\n\n" +
            "Printing will be enabled after the report is changed " +
            "to read STOCK_TRANSFER_HEADER / STOCK_TRANSFER_LINE.",
            "Print",
            MessageButton.OK,
            MessageIcon.Information);
    }


    private bool CanPrintStockTransfer()
    {
        return
            TransferGkey > 0 &&
            !string.IsNullOrWhiteSpace(
                TransferNbr);
    }


    // =========================================================
    // TOTALS
    // =========================================================

    public int TotalItems =>
        OmTransUIList.Count;


    public decimal TotalTransferWeight =>
        OmTransUIList.Sum(
            x => x.TransferWeight);


    private void OmTransUIList_CollectionChanged(
        object? sender,
        NotifyCollectionChangedEventArgs e)
    {
        NotifyTotals();


        CreateStockTransferCommand
            .NotifyCanExecuteChanged();
    }

    partial void OnCanEditPurchaseChanged(bool value)
    {
        CreateStockTransferCommand
            .NotifyCanExecuteChanged();

        PrintPreviewStockTransferCommand
            .NotifyCanExecuteChanged();

        PrintStockTransferCommand
            .NotifyCanExecuteChanged();
    }

    partial void OnIsBusyChanged(bool value)
    {
        CreateStockTransferCommand
            .NotifyCanExecuteChanged();

        PrintPreviewStockTransferCommand
            .NotifyCanExecuteChanged();

        PrintStockTransferCommand
            .NotifyCanExecuteChanged();
    }

    private void NotifyTotals()
    {
        OnPropertyChanged(
            nameof(TotalItems));

        OnPropertyChanged(
            nameof(TotalTransferWeight));
    }
}