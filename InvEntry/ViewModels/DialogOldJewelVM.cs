using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InvEntry.ViewModels
{
    public partial class DialogOldJewelVM : ObservableObject
    {
        [ObservableProperty]
        private decimal weight;

        [ObservableProperty]
        private decimal rate;
    }
}
