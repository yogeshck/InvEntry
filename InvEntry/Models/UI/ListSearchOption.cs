using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using CommunityToolkit.Mvvm.ComponentModel;

namespace InvEntry.Models.UI;

public partial class ListSearchOption : ObservableObject
{
    [ObservableProperty]
    private DateTime from =
        DateTime.Today.AddDays(-1);

    [ObservableProperty]
    private DateTime to =
        DateTime.Today;

    [ObservableProperty]
    private string? filterValue;
}
