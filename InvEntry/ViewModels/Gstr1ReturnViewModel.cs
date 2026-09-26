using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DevExpress.Mvvm;
using DevExpress.XtraPrinting;
using DevExpress.XtraReports.UI;
using DevExpress.XtraEditors.TextEditController.InputHandler;
using InvEntry.Contracts.Gst;
using InvEntry.Services;
using InvEntry.Extension;
using InvEntry.Models;
using InvEntry.Reports;
using System;
using System.Collections.Generic;
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
    private OrgThisCompanyView? _company;


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
    [NotifyCanExecuteChangedFor(nameof(PrintPreviewGstr1Command))]
    [NotifyCanExecuteChangedFor(nameof(PrintGstr1Command))]
    [NotifyCanExecuteChangedFor(nameof(ExportGstr1PdfCommand))]
    [NotifyCanExecuteChangedFor(nameof(ExportGstr1ExcelCommand))]
    private Gstr1ReturnSummaryResponse? _summary;


    // =========================================================
    // CATEGORY SUMMARY
    // =========================================================

    [ObservableProperty]
    private ObservableCollection<Gstr1CategorySummaryResponse>
        _categories = new();

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(PrintPreviewGstr1Command))]
    [NotifyCanExecuteChangedFor(nameof(PrintGstr1Command))]
    [NotifyCanExecuteChangedFor(nameof(ExportGstr1PdfCommand))]
    [NotifyCanExecuteChangedFor(nameof(ExportGstr1ExcelCommand))]
    private Gstr1CategorySummaryResponse? _selectedCategory;


    // =========================================================
    // DOCUMENTS
    // =========================================================

    [ObservableProperty]
    private ObservableCollection<Gstr1DocumentResponse>
        _documents = new();

    [ObservableProperty]
    private Gstr1DocumentResponse? _selectedDocument;

    [ObservableProperty]
    private string _documentsEmptyMessage =
        "Select a category above to view its documents.";

    [ObservableProperty]
    private bool _showDocumentsEmptyState = true;


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
    [NotifyCanExecuteChangedFor(nameof(PrintPreviewGstr1Command))]
    [NotifyCanExecuteChangedFor(nameof(PrintGstr1Command))]
    [NotifyCanExecuteChangedFor(nameof(ExportGstr1PdfCommand))]
    [NotifyCanExecuteChangedFor(nameof(ExportGstr1ExcelCommand))]
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
    [NotifyCanExecuteChangedFor(nameof(PrintPreviewGstr1Command))]
    [NotifyCanExecuteChangedFor(nameof(PrintGstr1Command))]
    [NotifyCanExecuteChangedFor(nameof(ExportGstr1PdfCommand))]
    [NotifyCanExecuteChangedFor(nameof(ExportGstr1ExcelCommand))]
    [NotifyPropertyChangedFor(nameof(CanChangeScope))]
    private bool _isExporting;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(PrintPreviewGstr1Command))]
    [NotifyCanExecuteChangedFor(nameof(PrintGstr1Command))]
    [NotifyCanExecuteChangedFor(nameof(ExportGstr1PdfCommand))]
    [NotifyCanExecuteChangedFor(nameof(ExportGstr1ExcelCommand))]
    [NotifyPropertyChangedFor(nameof(CanChangeScope))]
    private bool _isReporting;

    public IReadOnlyList<string> ReportScopes { get; } =
        ["Selected Category", "Complete Monthly Return"];

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(PrintPreviewGstr1Command))]
    [NotifyCanExecuteChangedFor(nameof(PrintGstr1Command))]
    [NotifyCanExecuteChangedFor(nameof(ExportGstr1PdfCommand))]
    [NotifyCanExecuteChangedFor(nameof(ExportGstr1ExcelCommand))]
    private string _selectedReportScope = "Complete Monthly Return";

    [ObservableProperty]
    private string? _exportStatusMessage;

    private bool _isLoadingReturn;
    private IReadOnlyList<Gstr1DocumentResponse> _monthlyDocuments = [];
    private int _returnLoadVersion;
    private int _documentLinesVersion;

    public bool CanChangeScope => !IsBusy && !IsExporting && !IsReporting;

    public string ValidationStatus => Validation is null
        ? "Validation unavailable — load the return."
        : $"{(Validation.IsExportReady ? "Export Ready" : "Export blocked")} — " +
          $"Errors: {Validation.ErrorCount}, Warnings: {Validation.WarningCount}, Information: {Validation.InformationCount}";

    private bool CanExportJson() => CanChangeScope &&
        !string.IsNullOrWhiteSpace(SupplierGstin) &&
        Validation?.IsExportReady == true &&
        Validation.SupplierGstin == SupplierGstin &&
        Validation.ReturnPeriod == ReturnPeriod;

    partial void OnSupplierGstinChanged(string value)
    {
        InvalidateValidation();
        InvalidateReturnScope();
    }

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

            _company = company;

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
        InvalidateReturnScope();

        OnPropertyChanged(
            nameof(ReturnPeriod));

        OnPropertyChanged(
            nameof(ReturnPeriodDisplay));
    }


    private void InvalidateReturnScope()
    {
        _returnLoadVersion++;
        _documentLinesVersion++;
        Summary = null;
        Categories.Clear();
        SelectedCategory = null;
        _monthlyDocuments = [];
        Documents.Clear();
        SelectedDocument = null;
        DocumentLines.Clear();
        DocumentsEmptyMessage = "Select a category above to view its documents.";
        ShowDocumentsEmptyState = true;
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
            return;

        if (string.IsNullOrWhiteSpace(SupplierGstin))
        {
            _messageBoxService.ShowMessage(
                "Supplier GSTIN is not available.", "GSTR-1",
                MessageButton.OK, MessageIcon.Warning);
            return;
        }

        var supplierGstin = SupplierGstin;
        var returnPeriod = ReturnPeriod;
        var loadVersion = ++_returnLoadVersion;
        try
        {
            _isLoadingReturn = true;
            IsBusy = true;
            InvalidateValidation();

            SelectedCategory = null;
            _monthlyDocuments = [];
            Documents.Clear();
            SelectedDocument = null;
            DocumentLines.Clear();
            DocumentsEmptyMessage = "Select a category above to view its documents.";
            ShowDocumentsEmptyState = true;
            StatusMessage = $"Loading GSTR-1 for {ReturnPeriodDisplay}...";

            var summaryTask = _gstr1ReportService.GetSummaryAsync(supplierGstin, returnPeriod);
            var documentsTask = _gstr1ReportService.GetDocumentsAsync(supplierGstin, returnPeriod);
            await Task.WhenAll(summaryTask, documentsTask);

            var validation = await _gstr1ReportService.GetValidationAsync(supplierGstin, returnPeriod);
            if (loadVersion != _returnLoadVersion || !ScopeMatches(supplierGstin, returnPeriod))
                return;

            Validation = validation;
            Summary = await summaryTask;
            Categories = new ObservableCollection<Gstr1CategorySummaryResponse>(Summary?.Categories ?? []);
            _monthlyDocuments = await documentsTask ?? [];

            // Documents remain hidden until the operator chooses the exact
            // ReturnCategory/Gstr1Table summary row.
            SelectedCategory = null;
            Documents.Clear();
            SelectedDocument = null;
            DocumentLines.Clear();
            DocumentsEmptyMessage = "Select a category above to view its documents.";
            ShowDocumentsEmptyState = true;
            StatusMessage = $"GSTR-1 summary loaded for {ReturnPeriodDisplay}. Select a category to view documents.";
        }
        catch (Exception ex)
        {
            if (loadVersion != _returnLoadVersion)
                return;

            Validation = null;
            Summary = null;
            Categories.Clear();
            SelectedCategory = null;
            _monthlyDocuments = [];
            Documents.Clear();
            SelectedDocument = null;
            DocumentLines.Clear();
            DocumentsEmptyMessage = "Select a category above to view its documents.";
            ShowDocumentsEmptyState = true;
            StatusMessage = "Unable to load GSTR-1 data.";
            _messageBoxService.ShowMessage(ex.Message, "GSTR-1", MessageButton.OK, MessageIcon.Error);
        }
        finally
        {
            _isLoadingReturn = false;
            IsBusy = false;
        }
    }

    // =========================================================
    // SELECTED CATEGORY
    // =========================================================

    partial void OnSelectedCategoryChanged(Gstr1CategorySummaryResponse? value)
    {
        _documentLinesVersion++;
        SelectedDocument = null;
        DocumentLines.Clear();
        Documents.Clear();

        if (value is null)
        {
            DocumentsEmptyMessage = "Select a category above to view its documents.";
            ShowDocumentsEmptyState = true;
            return;
        }

        var matches = _monthlyDocuments.Where(document =>
            string.Equals(document.ReturnCategory?.Trim(), value.Category?.Trim(),
                StringComparison.OrdinalIgnoreCase) &&
            string.Equals(document.Gstr1Table?.Trim(), value.Gstr1Table?.Trim(),
                StringComparison.OrdinalIgnoreCase));

        foreach (var document in matches)
            Documents.Add(document);

        ShowDocumentsEmptyState = Documents.Count == 0;
        DocumentsEmptyMessage = Documents.Count == 0
            ? "No documents found for the selected category."
            : string.Empty;
        StatusMessage = Documents.Count == 0
            ? $"No documents found for {value.Category}."
            : $"{Documents.Count:N0} {value.Category} document(s).";
    }

    // =========================================================
    // SELECTED DOCUMENT
    // =========================================================

    partial void OnSelectedDocumentChanged(
        Gstr1DocumentResponse? value)
    {
        _documentLinesVersion++;
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
        var requestVersion = _documentLinesVersion;
        var supplierGstin = SupplierGstin;
        var returnPeriod = ReturnPeriod;

        try
        {
            StatusMessage =
                $"Loading {document.DocumentNbr}...";

            var lines =
                await _gstr1ReportService
                    .GetDocumentLinesAsync(
                        document.Gkey,
                        supplierGstin,
                        returnPeriod);

            /*
             * Selection may have changed while the HTTP request
             * was running.
             *
             * Do not display lines belonging to an old selection.
             */
            if (requestVersion != _documentLinesVersion ||
                !ScopeMatches(supplierGstin, returnPeriod) ||
                SelectedDocument?.Gkey != document.Gkey)
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
            if (requestVersion == _documentLinesVersion &&
                ScopeMatches(supplierGstin, returnPeriod) &&
                SelectedDocument?.Gkey == document.Gkey)
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
    // HUMAN-READABLE REPORTING
    // =========================================================

    private bool CanGenerateReport() =>
        CanChangeScope &&
        Summary is not null &&
        (SelectedReportScope != "Selected Category" || SelectedCategory is not null);

    [RelayCommand(CanExecute = nameof(CanGenerateReport))]
    private void PrintPreviewGstr1() =>
        ExecuteReport(report => new ReportPrintTool(report).ShowRibbonPreviewDialog());

    [RelayCommand(CanExecute = nameof(CanGenerateReport))]
    private void PrintGstr1() =>
        ExecuteReport(report => new ReportPrintTool(report).PrintDialog());

    [RelayCommand(CanExecute = nameof(CanGenerateReport))]
    private void ExportGstr1Pdf()
    {
        var dialog = new Microsoft.Win32.SaveFileDialog
        {
            Title = "Export GSTR-1 PDF",
            FileName = GetReportFileName("pdf"),
            Filter = "PDF files (*.pdf)|*.pdf",
            DefaultExt = ".pdf",
            AddExtension = true,
            OverwritePrompt = true
        };

        if (dialog.ShowDialog() == true)
            ExecuteReport(report => report.ExportToPdf(dialog.FileName));
    }

    [RelayCommand(CanExecute = nameof(CanGenerateReport))]
    private void ExportGstr1Excel()
    {
        var dialog = new Microsoft.Win32.SaveFileDialog
        {
            Title = "Export GSTR-1 Excel",
            FileName = GetReportFileName("xlsx"),
            Filter = "Excel files (*.xlsx)|*.xlsx",
            DefaultExt = ".xlsx",
            AddExtension = true,
            OverwritePrompt = true
        };

        if (dialog.ShowDialog() == true)
        {
            ExecuteReport(report => report.ExportToXlsx(
                dialog.FileName,
                new XlsxExportOptions
                {
                    TextExportMode = TextExportMode.Value,
                    ShowGridLines = true
                }));
        }
    }

    private void ExecuteReport(Action<XtraReport> action)
    {
        if (!CanGenerateReport())
            return;

        try
        {
            IsReporting = true;
            StatusMessage = "Preparing GSTR-1 report...";
            using var report = new Gstr1MonthlyReport(CreateReportSnapshot());
            report.CreateDocument();
            action(report);
            StatusMessage = "GSTR-1 report completed.";
        }
        catch (Exception ex)
        {
            StatusMessage = "Unable to generate the GSTR-1 report.";
            _messageBoxService.ShowMessage(
                ex.Message,
                "GSTR-1 Report",
                MessageButton.OK,
                MessageIcon.Error);
        }
        finally
        {
            IsReporting = false;
        }
    }

    private Gstr1ReportSnapshot CreateReportSnapshot()
    {
        var summary = Summary ?? throw new InvalidOperationException("Load the GSTR-1 return before reporting.");
        var selectedScope = SelectedReportScope == "Selected Category";
        var categories = selectedScope
            ? new[] { SelectedCategory ?? throw new InvalidOperationException("Select a GSTR-1 category.") }
            : summary.Categories.ToArray();
        var monthlyDocuments = _monthlyDocuments.ToArray();
        var rows = new List<Gstr1ReportRow>();

        foreach (var category in categories)
        {
            rows.Add(new Gstr1ReportRow
            {
                RowType = "Category",
                Category = category.Category,
                Gstr1Table = category.Gstr1Table ?? string.Empty,
                DocumentCount = category.DocumentCount,
                InvoiceValue = category.InvoiceValue,
                TaxableValue = category.TaxableValue,
                CgstAmount = category.CgstAmount,
                SgstAmount = category.SgstAmount,
                IgstAmount = category.IgstAmount,
                CessAmount = category.CessAmount
            });

            foreach (var document in monthlyDocuments.Where(document =>
                document.IsReportable && CategoryMatches(document, category)))
            {
                rows.Add(new Gstr1ReportRow
                {
                    RowType = "Document",
                    Category = category.Category,
                    Gstr1Table = category.Gstr1Table ?? string.Empty,
                    DocumentNbr = document.DocumentNbr,
                    DocumentDate = document.DocumentDate.ToDateTime(TimeOnly.MinValue),
                    Recipient = !string.IsNullOrWhiteSpace(document.RecipientGstin)
                        ? document.RecipientGstin
                        : document.RecipientStateCode ?? string.Empty,
                    InvoiceValue = document.InvoiceValue,
                    TaxableValue = document.TaxableValue,
                    CgstAmount = document.CgstAmount,
                    SgstAmount = document.SgstAmount,
                    IgstAmount = document.IgstAmount,
                    CessAmount = document.CessAmount
                });
            }
        }

        return new Gstr1ReportSnapshot
        {
            CompanyName = CompanyName,
            Branch = GetBranchLocation(),
            SupplierGstin = SupplierGstin,
            FinancialYear = GetFinancialYear(ReturnMonth),
            ReturnMonth = ReturnPeriodDisplay,
            Scope = selectedScope
                ? $"Selected Category — {SelectedCategory!.Category} / Table {SelectedCategory.Gstr1Table ?? "-"}"
                : "Complete Monthly Return",
            GeneratedAt = DateTime.Now,
            DocumentCount = categories.Sum(category => category.DocumentCount),
            InvoiceValue = categories.Sum(category => category.InvoiceValue),
            TaxableValue = categories.Sum(category => category.TaxableValue),
            CgstAmount = categories.Sum(category => category.CgstAmount),
            SgstAmount = categories.Sum(category => category.SgstAmount),
            IgstAmount = categories.Sum(category => category.IgstAmount),
            CessAmount = categories.Sum(category => category.CessAmount),
            Rows = rows
        };
    }

    private static bool CategoryMatches(
        Gstr1DocumentResponse document,
        Gstr1CategorySummaryResponse category) =>
        string.Equals(document.ReturnCategory?.Trim(), category.Category?.Trim(), StringComparison.OrdinalIgnoreCase) &&
        string.Equals(document.Gstr1Table?.Trim(), category.Gstr1Table?.Trim(), StringComparison.OrdinalIgnoreCase);

    private string GetBranchLocation() =>
        string.Join(", ", new[] { _company?.City, _company?.District, _company?.State }
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Distinct(StringComparer.OrdinalIgnoreCase));

    private static string GetFinancialYear(DateTime month)
    {
        var startYear = month.Month >= 4 ? month.Year : month.Year - 1;
        return $"{startYear}-{(startYear + 1) % 100:00}";
    }

    private string GetReportFileName(string extension)
    {
        var scope = SelectedReportScope == "Selected Category"
            ? SelectedCategory?.Category ?? "Category"
            : "Monthly";
        return $"GSTR1_{SupplierGstin}_{ReturnMonth:MMyyyy}_{scope}.{extension}";
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