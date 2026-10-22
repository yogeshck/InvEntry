using CommunityToolkit.Mvvm.ComponentModel;
using InvEntry.Contracts.Invoices;

namespace InvEntry.ViewModels;

/// <summary>One receipt typed into the outstanding-invoice entry grid, not yet saved.</summary>
public partial class CollectionEntryLine : ObservableObject
{
    [ObservableProperty]
    private string? _paymentType;

    [ObservableProperty]
    private decimal _amount;

    [ObservableProperty]
    private string? _reference;

    public InvoiceCollectionLineModel ToModel() => new()
    {
        PaymentType = PaymentType ?? string.Empty,
        Amount = Amount,
        Reference = Reference
    };
}
