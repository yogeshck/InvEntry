using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using InvEntry.Models;
using InvEntry.Services;
using System;
using System.Threading.Tasks;

namespace InvEntry.ViewModels;

/// <summary>
/// Edits one company. Works on a copy, so the list only changes after the server accepts the save.
/// Account, tenant, address link and flags are kept from the original record.
/// </summary>
public partial class CompanyEditViewModel : ObservableObject
{
    private readonly IOrgCompanyService _orgCompanyService;
    private readonly OrgCompany _original;

    public CompanyEditViewModel(IOrgCompanyService orgCompanyService, OrgCompany company)
    {
        _orgCompanyService = orgCompanyService;
        _original = company;

        Name = company.Name;
        Tagline = company.Tagline;
        PanNbr = company.PanNbr;
        GstNbr = company.GstNbr;
        TinNbr = company.TinNbr;
        CinNbr = company.CinNbr;
        ServiceTaxNbr = company.ServiceTaxNbr;
        ContactNbr1 = company.ContactNbr1;
        ContactNbr2 = company.ContactNbr2;
        EmailId = company.EmailId;
        Notes = company.Notes;
        ThisCompany = company.ThisCompany ?? false;
        AddressGkey = company.AddressGkey;
    }

    /// <summary>Raised after a successful save, so the popup can close.</summary>
    public event EventHandler? SaveCompleted;

    public bool ThisCompany { get; }

    public long? AddressGkey { get; }

    [ObservableProperty]
    private string? _name;

    [ObservableProperty]
    private string? _tagline;

    [ObservableProperty]
    private string? _panNbr;

    [ObservableProperty]
    private string? _gstNbr;

    [ObservableProperty]
    private string? _tinNbr;

    [ObservableProperty]
    private string? _cinNbr;

    [ObservableProperty]
    private string? _serviceTaxNbr;

    [ObservableProperty]
    private string? _contactNbr1;

    [ObservableProperty]
    private string? _contactNbr2;

    [ObservableProperty]
    private string? _emailId;

    [ObservableProperty]
    private string? _notes;

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
            await _orgCompanyService.UpdateOrgCompany(BuildUpdated());
            SaveCompleted?.Invoke(this, EventArgs.Empty);
        }
        catch (Exception ex)
        {
            Error = $"Unable to save the company: {ex.Message}";
        }
        finally
        {
            Saving = false;
        }
    }

    private string? Validate()
    {
        if (string.IsNullOrWhiteSpace(Name))
            return "Company name is required.";

        if (!string.IsNullOrWhiteSpace(PanNbr) && PanNbr.Trim().Length != 10)
            return "PAN must be 10 characters.";

        if (!string.IsNullOrWhiteSpace(GstNbr) && GstNbr.Trim().Length != 15)
            return "GSTIN must be 15 characters.";

        if (!string.IsNullOrWhiteSpace(EmailId) && !EmailId.Contains('@'))
            return "Email address is not valid.";

        return null;
    }

    /// <summary>A new record with the edited fields and the original's other fields.</summary>
    private OrgCompany BuildUpdated() => new()
    {
        GKey = _original.GKey,
        AccountId = _original.AccountId,
        DraftId = _original.DraftId,
        InvId = _original.InvId,
        Status = _original.Status,
        DeleteFlag = _original.DeleteFlag,
        TenantGkey = _original.TenantGkey,
        AddressGkey = _original.AddressGkey,
        ThisCompany = _original.ThisCompany,
        CreatedBy = _original.CreatedBy,
        CreatedOn = _original.CreatedOn,
        Name = Trim(Name),
        Tagline = Trim(Tagline),
        PanNbr = Trim(PanNbr),
        GstNbr = Trim(GstNbr),
        TinNbr = Trim(TinNbr),
        CinNbr = Trim(CinNbr),
        ServiceTaxNbr = Trim(ServiceTaxNbr),
        ContactNbr1 = Trim(ContactNbr1),
        ContactNbr2 = Trim(ContactNbr2),
        EmailId = Trim(EmailId),
        Notes = Trim(Notes),
    };

    private static string? Trim(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
