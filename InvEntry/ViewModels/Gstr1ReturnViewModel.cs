using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DevExpress.Mvvm;
using DevExpress.XtraEditors.TextEditController.InputHandler;
using InvEntry.Contracts.Gst;
using InvEntry.Services;
using InvEntry.Extension;
using System;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;

namespace InvEntry.ViewModels;

public partial class Gstr1ReturnViewModel : ObservableObject
{
    // =========================================================
    // SERVICES
    // =========================================================

    private readonly IGstr1ReportService _gstr1ReportService;
    private readonly IOrgThisCompanyViewService _orgThisCompanyViewService;
    private readonly IMessageBoxService _messageBoxService;


    // =========================================================
    // RETURN SCOPE
    // =========================================================

    [ObservableProperty]
    private string _supplierGstin = string.Empty;

    [ObservableProperty]
    private string _companyName = string.Empty;

    [ObservableProperty]
    private DateTime _returnMonth =
        new(DateTime.Today.Year, DateTime.Today.Month, 1);


    // =========================================================
    // SUMMARY
    // =========================================================

    [ObservableProperty]
    private Gstr1ReturnSummaryResponse? _summary;


    // =========================================================
    // CATEGORY SUMMARY
    // =========================================================

    [ObservableProperty]
    private ObservableCollection<Gstr1CategorySummaryResponse>
        _categories = new();


    // =========================================================
    // DOCUMENTS
    // =========================================================

    [ObservableProperty]
    private ObservableCollection<Gstr1DocumentResponse>
        _documents = new();

    [ObservableProperty]
    private Gstr1DocumentResponse? _selectedDocument;


    // =========================================================
    // DOCUMENT LINES
    // =========================================================

    [ObservableProperty]
    private ObservableCollection<Gstr1DocumentLineResponse>
        _documentLines = new();


    // =========================================================
    // UI STATE
    // =========================================================

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(ExportJsonCommand))]
    [NotifyCanExecuteChangedFor(nameof(LoadReturnCommand))]
    [NotifyPropertyChangedFor(nameof(CanChangeScope))]
    private bool _isBusy;

    [ObservableProperty]
    private string? _statusMessage;


    // =========================================================
    // EXPORT STATE AND COMMAND
    // =========================================================

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(ExportJsonCommand))]
    [NotifyPropertyChangedFor(nameof(ValidationStatus))]
    private Gstr1ValidationResponse? _validation;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(ExportJsonCommand))]
    [NotifyCanExecuteChangedFor(nameof(LoadReturnCommand))]
    [NotifyPropertyChangedFor(nameof(CanChangeScope))]
    private bool _isExporting;

    [ObservableProperty]
    private string? _exportStatusMessage;

    private bool _isLoadingReturn;

    public bool CanChangeScope => !IsBusy && !IsExporting;

    public string ValidationStatus => Validation is null
        ? "Validation unavailable — load the return."
        : $"{(Validation.IsExportReady ? "Export Ready" : "Export blocked")} — " +
          $"Errors: {Validation.ErrorCount}, Warnings: {Validation.WarningCount}, Information: {Validation.InformationCount}";

    private bool CanExportJson() => CanChangeScope &&
        !string.IsNullOrWhiteSpace(SupplierGstin) &&
        Validation?.IsExportReady == true &&
        Validation.SupplierGstin == SupplierGstin &&
        Validation.ReturnPeriod == ReturnPeriod;

    partial void OnSupplierGstinChanged(string value) => InvalidateValidation();

    private void InvalidateValidation()
    {
        Validation = null;
        ExportStatusMessage = null;
        ExportJsonCommand.NotifyCanExecuteChanged();
    }

    private bool ScopeMatches(string supplierGstin, string returnPeriod) =>
        SupplierGstin == supplierGstin && ReturnPeriod == returnPeriod;

    [RelayCommand(CanExecute = nameof(CanExportJson))]
    private async Task ExportJson()
    {
        if (!CanExportJson())
            return;

        var supplierGstin = SupplierGstin;
        var returnPeriod = ReturnPeriod;
        var filingPeriod = ReturnMonth.ToString("MMyyyy", CultureInfo.InvariantCulture);
        try
        {
            IsExporting = true;
            IsBusy = true;
            ExportStatusMessage = "Checking validation and generating GSTN JSON...";
            Messenger.Default.Send(MessageType.WaitIndicator,
                WaitIndicatorVM.ShowIndicator(ExportStatusMessage));

            // Refresh the backend status immediately before export; the export endpoint
            // also validates again. Never reproduce its validation rules here.
            Validation = null;
            var validation = await _gstr1ReportService.GetValidationAsync(supplierGstin, returnPeriod);
            if (!ScopeMatches(supplierGstin, returnPeriod))
                return;
            Validation = validation;
            if (!validation.IsExportReady)
            {
                ExportStatusMessage = "Export blocked. Review the validation issues.";
                return;
            }

            var json = await _gstr1ReportService.GetExportJsonAsync(supplierGstin, returnPeriod);
            if (!ScopeMatches(supplierGstin, returnPeriod))
                return;

            Messenger.Default.Send(MessageType.WaitIndicator, WaitIndicatorVM.HideIndicator());
            var dialog = new Microsoft.Win32.SaveFileDialog
            {
                Title = "Save GSTN JSON",
                FileName = $"GSTR1_{supplierGstin}_{filingPeriod}.json",
                Filter = "JSON files (*.json)|*.json",
                DefaultExt = ".json",
                AddExtension = true,
                OverwritePrompt = true
            };
            if (dialog.ShowDialog() != true)
            {
                ExportStatusMessage = "Export cancelled. No file was saved.";
                return;
            }

            // Backend JSON is UTF-8. Preserve bytes, whitespace, numeric formatting,
            // and property order without adding a BOM.
            await System.IO.File.WriteAllBytesAsync(dialog.FileName, json);
            ExportStatusMessage = $"GSTN JSON exported successfully. {dialog.FileName}";
        }
        catch (Exception ex)
        {
            ExportStatusMessage = $"GSTN JSON export failed: {ex.Message}";
            _messageBoxService.ShowMessage(ex.Message, "GSTR-1 JSON Export",
                MessageButton.OK, MessageIcon.Error);
        }
        finally
        {
            Messenger.Default.Send(MessageType.WaitIndicator, WaitIndicatorVM.HideIndicator());
            IsBusy = false;
            IsExporting = false;
        }
    }


    public Gstr1ReturnViewModel(
        IGstr1ReportService gstr1ReportService,
        IOrgThisCompanyViewService orgThisCompanyViewService,
        IMessageBoxService messageBoxService)
    {
        _gstr1ReportService = gstr1ReportService;
        _orgThisCompanyViewService = orgThisCompanyViewService;
        _messageBoxService = messageBoxService;

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
            StatusMessage = "Loading GST company details...";

            var company =
                await _orgThisCompanyViewService
                    .GetOrgThisCompany();

            if (company is null)
            {
                StatusMessage =
                    "Company details could not be loaded.";

                return;
            }

            CompanyName =
                company.CompanyName ?? string.Empty;

            SupplierGstin =
                company.GstNbr?.Trim().ToUpperInvariant()
                ?? string.Empty;

            if (string.IsNullOrWhiteSpace(SupplierGstin))
            {
                StatusMessage =
                    "GSTIN is not configured for this company.";

                return;
            }

            await LoadReturnAsync();
        }
        catch (Exception ex)
        {
            StatusMessage =
                "Unable to initialise GSTR-1.";

            _messageBoxService.ShowMessage(
                ex.Message,
                "GSTR-1",
                MessageButton.OK,
                MessageIcon.Error);
        }
        finally
        {
            IsBusy = false;
        }
    }


    // =========================================================
    // RETURN PERIOD
    // =========================================================

    public string ReturnPeriod =>
        ReturnMonth.ToString(
            "yyyyMM",
            CultureInfo.InvariantCulture);

    public string ReturnPeriodDisplay =>
        ReturnMonth.ToString(
            "MMMM yyyy",
            CultureInfo.CurrentCulture);


    partial void OnReturnMonthChanged(
        DateTime value)
    {
        /*
         * Always normalise to the first day.
         *
         * The UI represents a GST return month,
         * not an individual transaction date.
         */
        var normalized =
            new DateTime(
                value.Year,
                value.Month,
                1);

        if (value != normalized)
        {
            ReturnMonth = normalized;
            return;
        }

        InvalidateValidation();

        OnPropertyChanged(
            nameof(ReturnPeriod));

        OnPropertyChanged(
            nameof(ReturnPeriodDisplay));
    }


    // =========================================================
    // LOAD RETURN
    // =========================================================

    [RelayCommand(CanExecute = nameof(CanChangeScope))]
    private async Task LoadReturn()
    {
        await LoadReturnAsync();
    }


    private async Task LoadReturnAsync()
    {
        if (_isLoadingReturn || IsExporting)
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(
                SupplierGstin))
        {
            _messageBoxService.ShowMessage(
                "Supplier GSTIN is not available.",
                "GSTR-1",
                MessageButton.OK,
                MessageIcon.Warning);

            return;
        }

        var supplierGstin = SupplierGstin;
        var returnPeriod = ReturnPeriod;
        try
        {
            _isLoadingReturn = true;
            IsBusy = true;
            InvalidateValidation();

            StatusMessage =
                $"Loading GSTR-1 for {ReturnPeriodDisplay}...";

            /*
             * Clear the drill-down first so that lines from the
             * previous return period are never shown against the
             * newly selected month.
             */
            SelectedDocument = null;
            DocumentLines.Clear();

            var summaryTask =
                _gstr1ReportService
                    .GetSummaryAsync(
                        supplierGstin,
                        returnPeriod);

            var documentsTask =
                _gstr1ReportService
                    .GetDocumentsAsync(
                        supplierGstin,
                        returnPeriod);

            await Task.WhenAll(
                summaryTask,
                documentsTask);

            var validation = await _gstr1ReportService.GetValidationAsync(supplierGstin, returnPeriod);
            if (!ScopeMatches(supplierGstin, returnPeriod))
                return;
            Validation = validation;

            Summary =
                await summaryTask;

            var documents =
                await documentsTask;


            // -----------------------------------------------------
            // CATEGORY SUMMARY
            // -----------------------------------------------------

            Categories =
                new ObservableCollection<Gstr1CategorySummaryResponse>(
                    Summary?.Categories
                    ?? []);


            // -----------------------------------------------------
            // DOCUMENTS
            // -----------------------------------------------------

            Documents =
                new ObservableCollection<Gstr1DocumentResponse>(
                    documents
                    ?? []);


            StatusMessage =
                Documents.Count == 0
                    ? $"No staged GSTR-1 documents found for " +
                      $"{ReturnPeriodDisplay}."
                    : $"{Documents.Count} staged document(s) loaded.";
        }
        catch (Exception ex)
        {
            Validation = null;
            Summary = null;
            Categories.Clear();
            Documents.Clear();
            DocumentLines.Clear();
            SelectedDocument = null;

            StatusMessage =
                "Unable to load GSTR-1 data.";

            _messageBoxService.ShowMessage(
                ex.Message,
                "GSTR-1",
                MessageButton.OK,
                MessageIcon.Error);
        }
        finally
        {
            _isLoadingReturn = false;
            IsBusy = false;
        }
    }


    // =========================================================
    // SELECTED DOCUMENT
    // =========================================================

    partial void OnSelectedDocumentChanged(
        Gstr1DocumentResponse? value)
    {
        DocumentLines.Clear();

        if (value is null ||
            IsBusy)
        {
            return;
        }

        _ = LoadDocumentLinesAsync(value);
    }


    // =========================================================
    // LOAD DOCUMENT LINES
    // =========================================================

    private async Task LoadDocumentLinesAsync(
        Gstr1DocumentResponse document)
    {
        try
        {
            StatusMessage =
                $"Loading {document.DocumentNbr}...";

            var lines =
                await _gstr1ReportService
                    .GetDocumentLinesAsync(
                        document.Gkey,
                        SupplierGstin,
                        ReturnPeriod);

            /*
             * Selection may have changed while the HTTP request
             * was running.
             *
             * Do not display lines belonging to an old selection.
             */
            if (SelectedDocument?.Gkey !=
                document.Gkey)
            {
                return;
            }

            DocumentLines =
                new ObservableCollection<Gstr1DocumentLineResponse>(
                    lines ?? []);

            StatusMessage =
                $"{document.DocumentNbr}: " +
                $"{DocumentLines.Count} line(s).";
        }
        catch (Exception ex)
        {
            /*
             * Only show the error if this is still the selected
             * document. Otherwise it belongs to an obsolete
             * request.
             */
            if (SelectedDocument?.Gkey ==
                document.Gkey)
            {
                DocumentLines.Clear();

                StatusMessage =
                    $"Unable to load lines for " +
                    $"{document.DocumentNbr}.";

                _messageBoxService.ShowMessage(
                    ex.Message,
                    "GSTR-1 Document",
                    MessageButton.OK,
                    MessageIcon.Error);
            }
        }
    }


    // =========================================================
    // CONVENIENCE TOTALS FOR UI
    // =========================================================

    public decimal TotalTax =>
        (Summary?.TotalCgst ?? 0M) +
        (Summary?.TotalSgst ?? 0M) +
        (Summary?.TotalIgst ?? 0M) +
        (Summary?.TotalCess ?? 0M);


    partial void OnSummaryChanged(
        Gstr1ReturnSummaryResponse? value)
    {
        OnPropertyChanged(
            nameof(TotalTax));
    }
}