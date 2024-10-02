using CommunityToolkit.Mvvm.ComponentModel;
using InvEntry.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InvEntry.Services;

public interface IMasterDataService : IInitializableAsync
{
    ObservableCollection<ProductCategory> ProductCategories { get; }
}
public partial class MasterDataService : ObservableObject, IMasterDataService
{
    private readonly IProductCategoryService _productCategoryService;

    public MasterDataService(IProductCategoryService productCategoryService)
    {
        _productCategoryService = productCategoryService;
    }

    [ObservableProperty]
    public ObservableCollection<ProductCategory> _ProductCategories;

    public async Task InitAsync()
    {
        ProductCategories = new(await _productCategoryService.GetProductCategoryList());
    }
}
