using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InvEntry.ViewModels;

public partial class InvoiceReceiptsViewModel: ObservableObject
{
    [ObservableProperty]
    private string _transactionType;

    [ObservableProperty]
    private decimal _transactionAmount;

/*    [RelayCommand]
    private void AddTransaction()
    {

    }*/
}
