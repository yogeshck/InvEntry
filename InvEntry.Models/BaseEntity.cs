using CommunityToolkit.Mvvm.ComponentModel;

namespace InvEntry.Models;

public partial class BaseEntity : ObservableObject
{
    [ObservableProperty]
    private long id;

    [ObservableProperty]
    private DateTime createdOn;

    [ObservableProperty]
    private string? createdBy;

    [ObservableProperty]
    private DateTime modifiedOn;

    [ObservableProperty]
    private string? modifiedBy;
}