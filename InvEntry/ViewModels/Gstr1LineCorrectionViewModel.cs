using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using InvEntry.Contracts.Gst;
using InvEntry.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

namespace InvEntry.ViewModels;

/// <summary>
/// Popup for correcting the GST reporting fields (HSN, UOM, UQC,
/// GST quantity) of one staged GSTR-1 line flagged by validation.
/// </summary>
public partial class Gstr1LineCorrectionViewModel : ObservableObject
{
    private static readonly string[] CommonUoms =
        ["Grams", "Kilograms", "Pieces", "Numbers", "Sets", "Pairs"];

    private readonly IGstr1ReportService _gstr1ReportService;

    private string _supplierGstin = string.Empty;
    private string _returnPeriod = string.Empty;
    private (string? Hsn, string? Uom, string? Uqc, decimal? Qty) _original;
    private bool _initializing;

    public Gstr1LineCorrectionViewModel(IGstr1ReportService gstr1ReportService)
    {
        _gstr1ReportService = gstr1ReportService;
    }

    // ---------------------------------------------------------
    // Context
    // ---------------------------------------------------------

    [ObservableProperty]
    private Gstr1ValidationIssueResponse? _issue;

    [ObservableProperty]
    private Gstr1LineCorrectionResponse? _line;

    public IReadOnlyList<Gstr1UqcOption> UqcOptions => GstUqc.Options;

    [ObservableProperty]
    private IReadOnlyList<string> _uomOptions = CommonUoms;

    // ---------------------------------------------------------
    // Editable values
    // ---------------------------------------------------------

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasChanges))]
    private string? _hsnCode;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasChanges))]
    [NotifyPropertyChangedFor(nameof(UomHint))]
    private string? _uom;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasChanges))]
    [NotifyPropertyChangedFor(nameof(UomHint))]
    private string? _uqc;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasChanges))]
    private decimal? _gstQuantity;

    public string? UomHint
    {
        get
        {
            var expected = GstUqc.MapFromUom(Uom);
            if (expected is null)
                return string.IsNullOrWhiteSpace(Uom)
                    ? null
                    : $"No automatic UQC for '{Uom}'. Choose the UQC.";

            return GstUqc.IsMismatch(Uom, Uqc)
                ? $"'{Uom}' must be reported as {expected}."
                : $"'{Uom}' is reported as {expected}.";
        }
    }

    // ---------------------------------------------------------
    // UI state / result
    // ---------------------------------------------------------

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SaveCommand))]
    [NotifyCanExecuteChangedFor(nameof(ApplySuggestionCommand))]
    private bool _isBusy;

    [ObservableProperty]
    private string? _errorMessage;

    public bool HasChanges =>
        Line is not null &&
        (!SameText(HsnCode, _original.Hsn) ||
         !SameText(Uom, _original.Uom) ||
         !SameText(Uqc, _original.Uqc) ||
         GstQuantity != _original.Qty);

    public bool WasSaved { get; private set; }

    public event EventHandler? Saved;

    public event EventHandler? Cancelled;

    // ---------------------------------------------------------
    // Initialize
    // ---------------------------------------------------------

    public async Task InitializeAsync(
        Gstr1ValidationIssueResponse issue,
        string supplierGstin,
        string returnPeriod)
    {
        ArgumentNullException.ThrowIfNull(issue);

        if (issue.DocumentGkey is not > 0 || issue.LineNbr is null)
            throw new ArgumentException("The validation issue does not identify a document line.", nameof(issue));

        Issue = issue;
        _supplierGstin = supplierGstin;
        _returnPeriod = returnPeriod;
        WasSaved = false;
        ErrorMessage = null;

        var line = await _gstr1ReportService.GetLineCorrectionAsync(
            issue.DocumentGkey.Value, issue.LineNbr.Value, supplierGstin, returnPeriod);

        LoadLine(line);
    }

    private void LoadLine(Gstr1LineCorrectionResponse line)
    {
        try
        {
            _initializing = true;

            Line = line;
            _original = (line.HsnCode, line.Uom, line.Uqc, line.GstQuantity);

            UomOptions = CommonUoms
                .Concat(new[] { line.ProductUom, line.Uom })
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Select(x => x!.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            // Start from the suggested fix; the operator reviews it before saving.
            HsnCode = line.SuggestedHsnCode ?? line.HsnCode;
            Uom = line.SuggestedUom ?? line.Uom;
            Uqc = line.SuggestedUqc ?? line.Uqc;
            GstQuantity = line.SuggestedGstQuantity ?? line.GstQuantity;
        }
        finally
        {
            _initializing = false;
        }

        OnPropertyChanged(nameof(HasChanges));
    }

    // ---------------------------------------------------------
    // Dependent field suggestions
    // ---------------------------------------------------------

    partial void OnUomChanged(string? value)
    {
        if (_initializing)
            return;

        var mapped = GstUqc.MapFromUom(value);
        if (mapped is not null)
            Uqc = mapped;
    }

    partial void OnUqcChanged(string? oldValue, string? newValue)
    {
        if (_initializing || Line is null)
            return;

        // Only replace a quantity that was itself derived from the previous UQC,
        // never one the operator typed.
        var previousSuggestion = GstUqc.SuggestGstQuantity(oldValue, Line.Quantity, Line.NetWeight);
        if (GstQuantity is null || GstQuantity == previousSuggestion || GstQuantity == Line.GstQuantity)
        {
            GstQuantity = GstUqc.SuggestGstQuantity(newValue, Line.Quantity, Line.NetWeight)
                ?? GstQuantity;
        }
    }

    // ---------------------------------------------------------
    // Commands
    // ---------------------------------------------------------

    private bool CanEdit() => !IsBusy && Line is not null;

    [RelayCommand(CanExecute = nameof(CanEdit))]
    private void ApplySuggestion()
    {
        if (Line is null)
            return;

        LoadLine(Line);
        ErrorMessage = null;
    }

    [RelayCommand(CanExecute = nameof(CanEdit))]
    private async Task SaveAsync()
    {
        if (Line is null)
            return;

        ErrorMessage = Validate();
        if (ErrorMessage is not null)
            return;

        try
        {
            IsBusy = true;

            var saved = await _gstr1ReportService.SaveLineCorrectionAsync(
                Line.DocumentGkey,
                Line.LineNbr,
                new Gstr1LineCorrectionRequest
                {
                    SupplierGstin = _supplierGstin,
                    ReturnPeriod = _returnPeriod,
                    HsnCode = HsnCode?.Trim(),
                    Uom = Uom?.Trim(),
                    Uqc = Uqc?.Trim().ToUpperInvariant(),
                    GstQuantity = GstQuantity
                });

            LoadLine(saved);
            WasSaved = true;
            Saved?.Invoke(this, EventArgs.Empty);
        }
        catch (Exception ex)
        {
            ErrorMessage = ReadServerError(ex);
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private void Cancel()
    {
        ErrorMessage = null;
        Cancelled?.Invoke(this, EventArgs.Empty);
    }

    // ---------------------------------------------------------
    // Validation (the server validates again before saving)
    // ---------------------------------------------------------

    public string? Validate()
    {
        var errors = new List<string>();

        var hsn = HsnCode?.Trim();
        if (string.IsNullOrEmpty(hsn))
            errors.Add("HSN code is required.");
        else if (!hsn.All(char.IsAsciiDigit) || hsn.Length is not (4 or 6 or 8))
            errors.Add("HSN code must be 4, 6 or 8 digits.");

        if (string.IsNullOrWhiteSpace(Uom))
            errors.Add("UOM is required.");

        if (string.IsNullOrWhiteSpace(Uqc))
            errors.Add("GST UQC is required.");
        else if (!GstUqc.IsValid(Uqc))
            errors.Add($"'{Uqc}' is not a valid GST UQC.");
        else if (GstUqc.IsMismatch(Uom, Uqc))
            errors.Add($"UOM '{Uom}' must be reported as UQC '{GstUqc.MapFromUom(Uom)}'.");

        if (GstQuantity is not > 0M)
            errors.Add("GST quantity must be greater than zero.");
        else if (GstQuantity.Value != Math.Round(GstQuantity.Value, 3))
            errors.Add("GST quantity cannot have more than 3 decimal places.");

        return errors.Count == 0 ? null : string.Join(Environment.NewLine, errors);
    }

    private static string ReadServerError(Exception ex)
    {
        // MijmsApiService reports "PUT '<url>' failed: <status> - <body>".
        if (ex is HttpRequestException && ex.Message.IndexOf(" - {", StringComparison.Ordinal) is var at and >= 0)
        {
            try
            {
                using var json = JsonDocument.Parse(ex.Message[(at + 3)..]);
                if (json.RootElement.TryGetProperty("error", out var error))
                    return error.GetString() ?? ex.Message;
            }
            catch (JsonException)
            {
            }
        }

        return ex.Message;
    }

    private static bool SameText(string? left, string? right) =>
        string.Equals(left?.Trim() ?? string.Empty, right?.Trim() ?? string.Empty,
            StringComparison.OrdinalIgnoreCase);
}
