using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using InvEntry.Models;
using InvEntry.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InvEntry.ViewModels
{
    public partial class ProductStockViewModel : ObservableObject
    {
        [ObservableProperty]
        private Product _product;

        private readonly IProductService _productService;

        public ProductStockViewModel(IProductService productService)
        {
            _productService = productService;
        }

        [RelayCommand]
        private async Task Submit()
        {
            await _productService.CreateProduct(Product);
        }
    }
}
