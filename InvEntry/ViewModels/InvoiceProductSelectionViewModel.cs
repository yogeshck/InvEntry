using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using InvEntry.Models;
using DevExpress.Xpf.Grid;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using InvEntry.Services;
using Microsoft.Extensions.DependencyInjection;
using CommunityToolkit.Mvvm.Input;

namespace InvEntry.ViewModels
{
    public partial class InvoiceProductSelectionViewModel : ObservableObject
    {
        private readonly IProductViewService _productViewService;
        // private readonly IDialogService _reportDialogService;

        [ObservableProperty]
        private ObservableCollection<ProductView> _productStockView;

        [ObservableProperty]
        private ProductView _SelectedProduct;

        [ObservableProperty]
        private string _productSku;

        [ObservableProperty]
        private string _category;

        [ObservableProperty]
        private string _id;

        [ObservableProperty]
        private string _name;

        [ObservableProperty]
        private string _purity;

        [ObservableProperty]
        private decimal _gross_weight;

        [ObservableProperty]
        private decimal _stone_weight;

        [ObservableProperty]
        private decimal _net_weight;

        public InvoiceProductSelectionViewModel(IProductViewService productViewService)
        {
            _productViewService = productViewService;
            //_reportDialogService = reportDialogService;
            //_searchOption = new();
            //SearchOption.To = Today;
            //SearchOption.From = Today.AddDays(-1);
        }

        partial void OnCategoryChanged(string value)
        {
            Task.Run(async () => await RefreshProductStockAsync());
        }

        [RelayCommand]
        private async Task RefreshProductStockAsync()
        {
            var productStockResult = await _productViewService.GetByCategory(Category);
            if (productStockResult is not null)
                ProductStockView = new(productStockResult);
        }

    }
}
