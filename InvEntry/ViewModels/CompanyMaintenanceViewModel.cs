using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using InvEntry.Models;
using InvEntry.Services;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

namespace InvEntry.ViewModels;

/// <summary>Company master list. Editing is done in <see cref="CompanyEditViewModel"/>.</summary>
public partial class CompanyMaintenanceViewModel : ObservableObject
{
    private readonly IOrgCompanyService _orgCompanyService;

    public CompanyMaintenanceViewModel(IOrgCompanyService orgCompanyService)
    {
        _orgCompanyService = orgCompanyService;
    }

    [ObservableProperty]
    private ObservableCollection<OrgCompany> _companies = [];

    [ObservableProperty]
    private OrgCompany? _selectedCompany;

    [ObservableProperty]
    private bool _loading;

    [ObservableProperty]
    private string? _error;

    [RelayCommand]
    public async Task RefreshAsync()
    {
        Loading = true;
        Error = null;
        try
        {
            var items = (await _orgCompanyService.GetAll())
                .OrderByDescending(x => x.ThisCompany ?? false)
                .ThenBy(x => x.Name)
                .ToList();
            Companies = new ObservableCollection<OrgCompany>(items);
        }
        catch (Exception ex)
        {
            Error = $"Unable to load companies: {ex.Message}";
        }
        finally
        {
            Loading = false;
        }
    }

    /// <summary>Builds the edit form for the selected company, with its own copy of the data.</summary>
    public CompanyEditViewModel? CreateEditor() =>
        SelectedCompany is null
            ? null
            : new CompanyEditViewModel(_orgCompanyService, SelectedCompany);
}
