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
/// Product list with a category filter. Editing is done in <see cref="ProductEditViewModel"/>.
/// </summary>
public partial class ProductMaintenanceViewModel : ObservableObject
{
    public const string AllCategories = "All categories";

    private readonly IProductService _productService;
    private List<Product> _allProducts = [];

    public ProductMaintenanceViewModel(IProductService productService)
    {
        _productService = productService;
    }

    [ObservableProperty]
    private ObservableCollection<Product> _products = [];

    [ObservableProperty]
    private ObservableCollection<string> _categories = [AllCategories];

    [ObservableProperty]
    private string _selectedCategory = AllCategories;

    [ObservableProperty]
    private Product? _selectedProduct;

    [ObservableProperty]
    private bool _loading;

    [ObservableProperty]
    private string? _error;

    partial void OnSelectedCategoryChanged(string value) => ApplyFilter();

    [RelayCommand]
    public async Task RefreshAsync()
    {
        Loading = true;
        Error = null;
        try
        {
            _allProducts = (await _productService.GetAll())
                .OrderBy(x => x.Category)
                .ThenBy(x => x.Id)
                .ToList();

            var categories = _allProducts
                .Select(x => x.Category)
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(x => x, StringComparer.OrdinalIgnoreCase);

            Categories = new ObservableCollection<string>(
                new[] { AllCategories }.Concat(categories!));

            if (!Categories.Contains(SelectedCategory))
                SelectedCategory = AllCategories;

            ApplyFilter();
        }
        catch (Exception ex)
        {
            Error = $"Unable to load products: {ex.Message}";
        }
        finally
        {
            Loading = false;
        }
    }

    /// <summary>Builds the edit form for the selected product, with its own copy of the data.</summary>
    public ProductEditViewModel? CreateEditor()
    {
        if (SelectedProduct is null)
            return null;

        return new ProductEditViewModel(
            _productService,
            SelectedProduct,
            Categories.Where(x => x != AllCategories));
    }

    private void ApplyFilter()
    {
        var filtered = SelectedCategory == AllCategories
            ? _allProducts
            : _allProducts.Where(x =>
                string.Equals(x.Category, SelectedCategory, StringComparison.OrdinalIgnoreCase)).ToList();

        Products = new ObservableCollection<Product>(filtered);
        SelectedProduct = null;
    }
}
