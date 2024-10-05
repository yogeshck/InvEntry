using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InvEntry.Utils.Options
{
    public partial class InvoiceSearchOption : ObservableObject
    {
        [ObservableProperty]
        private DateTime _From;

        [ObservableProperty]
        private DateTime _To;
    }
}
