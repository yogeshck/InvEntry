using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using InvEntry.Models;
using InvEntry.Services;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

namespace InvEntry.ViewModels;

/// <summary>Voucher types list. Editing is done in <see cref="VoucherTypeEditViewModel"/>.</summary>
public partial class VoucherTypeMaintenanceViewModel : ObservableObject
{
    private readonly IVoucherTypeService _voucherTypeService;

    public VoucherTypeMaintenanceViewModel(IVoucherTypeService voucherTypeService)
    {
        _voucherTypeService = voucherTypeService;
    }

    [ObservableProperty]
    private ObservableCollection<VoucherType> _voucherTypes = [];

    [ObservableProperty]
    private VoucherType? _selectedVoucherType;

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
            var items = (await _voucherTypeService.GetAll())
                .OrderBy(x => x.DocumentType)
                .ToList();
            VoucherTypes = new ObservableCollection<VoucherType>(items);
        }
        catch (Exception ex)
        {
            Error = $"Unable to load voucher types: {ex.Message}";
        }
        finally
        {
            Loading = false;
        }
    }

    /// <summary>Builds the edit form for the selected voucher type, with its own copy of the data.</summary>
    public VoucherTypeEditViewModel? CreateEditor() =>
        SelectedVoucherType is null
            ? null
            : new VoucherTypeEditViewModel(_voucherTypeService, SelectedVoucherType);
}
