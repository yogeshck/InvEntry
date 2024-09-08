using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tern.MI.InvEntry.Models;

namespace InvEntry.Models
{
    public partial class Product : BaseEntity
    {
        [ObservableProperty]
        private string productName;

        [ObservableProperty]
        private double grossAmount;

        [ObservableProperty]
        private long productGkey;
    }
}
