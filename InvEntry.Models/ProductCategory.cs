using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InvEntry.Models
{
    public partial class ProductCategory : BaseEntity
    {

        [ObservableProperty]
        private int? _sn;

        [ObservableProperty]
        private string? _name;
      
    }
}
