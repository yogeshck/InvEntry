using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using InvEntry.Models;
using InvEntry.Services;
using System;
using System.Threading.Tasks;

namespace InvEntry.ViewModels;

/// <summary>
/// Edits one voucher type. The document type (its key) and the last used number are read-only here.
/// Works on a copy, so the list only changes after the server accepts the save.
/// </summary>
public partial class VoucherTypeEditViewModel : ObservableObject
{
    private readonly IVoucherTypeService _voucherTypeService;
    private readonly VoucherType _original;

    public VoucherTypeEditViewModel(IVoucherTypeService voucherTypeService, VoucherType voucherType)
    {
        _voucherTypeService = voucherTypeService;
        _original = voucherType;

        DocumentType = voucherType.DocumentType;
        LastUsedNumber = voucherType.LastUsedNumber;
        Abbreviation = voucherType.Abbreviation;
        Narration = voucherType.Narration;
        IsTaxable = voucherType.IsTaxable ?? false;
        IsActive = voucherType.IsActive ?? true;
        UsedFor = voucherType.UsedFor;
        DocNbrMethod = voucherType.DocNbrMethod;
        DocNbrLength = voucherType.DocNbrLength;
        DocNbrPrefill = voucherType.DocNbrPrefill;
        DocNbrPrefix = voucherType.DocNbrPrefix;
        DocNbrSuffix = voucherType.DocNbrSuffix;
    }

    /// <summary>Raised after a successful save, so the popup can close.</summary>
    public event EventHandler? SaveCompleted;

    public string? DocumentType { get; }

    public int? LastUsedNumber { get; }

    [ObservableProperty]
    private string? _abbreviation;

    [ObservableProperty]
    private string? _narration;

    [ObservableProperty]
    private bool _isTaxable;

    [ObservableProperty]
    private bool _isActive;

    [ObservableProperty]
    private string? _usedFor;

    [ObservableProperty]
    private string? _docNbrMethod;

    [ObservableProperty]
    private int? _docNbrLength;

    [ObservableProperty]
    private string? _docNbrPrefill;

    [ObservableProperty]
    private string? _docNbrPrefix;

    [ObservableProperty]
    private string? _docNbrSuffix;

    [ObservableProperty]
    private string? _error;

    [ObservableProperty]
    private bool _saving;

    [RelayCommand]
    private async Task SaveAsync()
    {
        Error = Validate();
        if (Error is not null)
            return;

        Saving = true;
        try
        {
            await _voucherTypeService.UpdateVoucherType(BuildUpdated());
            SaveCompleted?.Invoke(this, EventArgs.Empty);
        }
        catch (Exception ex)
        {
            Error = $"Unable to save the voucher type: {ex.Message}";
        }
        finally
        {
            Saving = false;
        }
    }

    private string? Validate()
    {
        if (string.IsNullOrWhiteSpace(Abbreviation))
            return "Abbreviation is required.";

        if (DocNbrLength is not null and (< 1 or > 30))
            return "Number length must be between 1 and 30.";

        return null;
    }

    /// <summary>
    /// A new record with the edited fields. Key and server-managed fields are copied from the original.
    /// </summary>
    private VoucherType BuildUpdated() => new()
    {
        GKey = _original.GKey,
        DocumentType = _original.DocumentType,
        MtblVoucherTypeGkey = _original.MtblVoucherTypeGkey,
        LastUsedNumber = _original.LastUsedNumber,
        CreatedBy = _original.CreatedBy,
        CreatedOn = _original.CreatedOn,
        Abbreviation = Trim(Abbreviation),
        Narration = Trim(Narration),
        IsTaxable = IsTaxable,
        IsActive = IsActive,
        UsedFor = Trim(UsedFor),
        DocNbrMethod = Trim(DocNbrMethod),
        DocNbrLength = DocNbrLength,
        DocNbrPrefill = Trim(DocNbrPrefill),
        DocNbrPrefix = Trim(DocNbrPrefix),
        DocNbrSuffix = Trim(DocNbrSuffix),
    };

    private static string? Trim(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
