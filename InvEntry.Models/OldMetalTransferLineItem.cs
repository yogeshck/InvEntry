using CommunityToolkit.Mvvm.ComponentModel;

namespace InvEntry.Models;

public partial class OldMetalTransferLineItem
    : ObservableObject
{
    [ObservableProperty]
    private int _productGkey;

    [ObservableProperty]
    private string? _productId;

    [ObservableProperty]
    private string? _productCategory;

    [ObservableProperty]
    private string? _metal;

    [ObservableProperty]
    private string? _purity;

    [ObservableProperty]
    private string? _uom;

    /// <summary>
    /// Actual available old-metal NET weight before this
    /// line is added.
    /// </summary>
    [ObservableProperty]
    private decimal _currentStock;

    /// <summary>
    /// Net weight being transferred for melting.
    /// </summary>
    [ObservableProperty]
    private decimal _transferWeight;

    /// <summary>
    /// Available stock after this transfer line.
    /// </summary>
    [ObservableProperty]
    private decimal _balanceAfterTransfer;

    [ObservableProperty]
    private string? _notes;
}
