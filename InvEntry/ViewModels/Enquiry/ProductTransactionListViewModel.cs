using CommunityToolkit.Mvvm.ComponentModel;
using InvEntry.Models;
using InvEntry.Services;
using InvEntry.Utils.Options;   
using IDialogService = DevExpress.Mvvm.IDialogService;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.ObjectModel;
using Microsoft.Extensions.DependencyInjection;

namespace InvEntry.ViewModels.Enquiry;

public partial class ProductTransactionListViewModel : ObservableObject
{
    private readonly IProductTransactionService _prodTransService;
    private readonly IDialogService _reportDialogService;

    [ObservableProperty]
    private ObservableCollection<ProductTransaction> _productTransactions;

    [ObservableProperty]
    private DateSearchOption _searchOption;

    [ObservableProperty]
    private ObservableCollection<string> _productCategoryList;

    [ObservableProperty]
    private DateTime _Today = DateTime.Today;

    public ProductTransactionListViewModel(
                                            IProductTransactionService prodTransService, 
                                            IDialogService reportDialogService )
                                         //  [FromKeyedServices("ReportDialogService")] IDialogService reportDialogService)
    {
        _prodTransService = prodTransService;
    //    _reportDialogService = reportDialogService;

        _searchOption = new();
        SearchOption.To = Today;
        SearchOption.From = Today.AddDays(-2);
        SearchOption.Filter1 ??= "Cash";

    }

}
