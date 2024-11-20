using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using InvEntry.Models;
using System.Collections.ObjectModel;
using System.Text.Json.Serialization;


namespace InvEntry.Models;

public partial class GrnHeader : BaseEntity
{

    public GrnHeader()
    {
        grnLineSumry = new();
        grnLines = new();
    }

    [ObservableProperty]
    public string? _grnNbr;

    [ObservableProperty]
    public DateTime? _grnDate;

    [ObservableProperty]
    public string? _documentType;

    [ObservableProperty]
    public string? _orderNbr;

    [ObservableProperty]
    public DateTime? _orderDate;

    [ObservableProperty]
    public string? _supplierId;
    
    [ObservableProperty]
    public string? _supplierRefNbr;

    [ObservableProperty]
    public string? _customerOrderNbr;

    [ObservableProperty]
    public string? _documentRef;

    [ObservableProperty]
    public DateTime? _documentDate;

    [ObservableProperty]
    [property: JsonIgnore]
    private ObservableCollection<GrnLine>? grnLines;

    [ObservableProperty]
    [property: JsonIgnore]
    private ObservableCollection<GrnLineSummary>? grnLineSumry;

}
