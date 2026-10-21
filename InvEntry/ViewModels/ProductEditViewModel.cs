using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using InvEntry.Models;
using InvEntry.Services;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

namespace InvEntry.ViewModels;

/// <summary>
/// Edits one product in a popup. Works on its own copy, so the list is only changed after a successful save.
/// </summary>
public partial class ProductEditViewModel : ObservableObject
{
    private readonly IProductService _productService;
    private readonly Product _original;

    public ProductEditViewModel(IProductService productService, Product product, IEnumerable<string> categories)
    {
        _productService = productService;
        _original = product;

        Id = product.Id;
        Name = product.Name;
        Description = product.Description;
        Category = product.Category;
        Metal = product.Metal;
        Purity = product.Purity;
        PurityPercent = product.PurityPercent;
        Brand = product.Brand;
        Model = product.Model;
        HsnCode = product.HsnCode;
        Uom = product.Uom;
        BaseUnit = product.BaseUnit;
        TaxRule = product.TaxRule;
        StockGroup = product.StockGroup;
        IsTaxable = product.IsTaxable ?? false;
        IsActive = product.IsActive ?? true;

        Categories = new ObservableCollection<string>(categories);
    }

    /// <summary>Raised after a successful save, so the popup can close.</summary>
    public event EventHandler? SaveCompleted;

    public ObservableCollection<string> Categories { get; }

    [ObservableProperty]
    private string? _id;

    [ObservableProperty]
    private string? _name;

    [ObservableProperty]
    private string? _description;

    [ObservableProperty]
    private string? _category;

    [ObservableProperty]
    private string? _metal;

    [ObservableProperty]
    private string? _purity;

    [ObservableProperty]
    private decimal? _purityPercent;

    [ObservableProperty]
    private string? _brand;

    [ObservableProperty]
    private string? _model;

    [ObservableProperty]
    private string? _hsnCode;

    [ObservableProperty]
    private string? _uom;

    [ObservableProperty]
    private string? _baseUnit;

    [ObservableProperty]
    private string? _taxRule;

    [ObservableProperty]
    private string? _stockGroup;

    [ObservableProperty]
    private bool _isTaxable;

    [ObservableProperty]
    private bool _isActive;

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
            await _productService.UpdateProduct(BuildUpdated());
            SaveCompleted?.Invoke(this, EventArgs.Empty);
        }
        catch (Exception ex)
        {
            Error = $"Unable to save the product: {ex.Message}";
        }
        finally
        {
            Saving = false;
        }
    }

    private string? Validate()
    {
        if (string.IsNullOrWhiteSpace(Name))
            return "Product name is required.";

        if (PurityPercent is < 0 or > 100)
            return "Purity % must be between 0 and 100.";

        return null;
    }

    /// <summary>
    /// A new record with the edited fields. Fields not on this form are copied from the original,
    /// and the original stays unchanged, so the list is only updated after the server accepts the save.
    /// </summary>
    private Product BuildUpdated() => new()
    {
        GKey = _original.GKey,
        Id = _original.Id,
        Name = Trim(Name),
        Description = Trim(Description),
        Category = Trim(Category),
        Metal = Trim(Metal),
        Purity = Trim(Purity),
        PurityPercent = PurityPercent,
        Brand = Trim(Brand),
        Model = Trim(Model),
        HsnCode = Trim(HsnCode),
        Uom = Trim(Uom),
        BaseUnit = Trim(BaseUnit),
        TaxRule = Trim(TaxRule),
        StockGroup = Trim(StockGroup),
        IsTaxable = IsTaxable,
        IsActive = IsActive,
        ImageRef = _original.ImageRef,
        SetGkey = _original.SetGkey,
        ProductAttributeGkey = _original.ProductAttributeGkey,
        CreatedBy = _original.CreatedBy,
        CreatedOn = _original.CreatedOn,
    };

    private static string? Trim(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
