using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InvEntry.Models
{
    public partial class GrnLineSummary : BaseEntity
    {

        [ObservableProperty]
        public int? _grnHdrGkey;

        [ObservableProperty]
        public int? _lineNbr;

        [ObservableProperty]
        public int? _productGkey;

        [ObservableProperty]
        public decimal? _grossWeight;

        [ObservableProperty]
        public decimal? _stoneWeight;

        [ObservableProperty]
        public decimal? _netWeight;

        [ObservableProperty]
        public int? _suppliedQty;

        [ObservableProperty]
        public string? _productCategory;

        [ObservableProperty]
        public string? _productPurity;

        [ObservableProperty]
        public string? _uom;


    }
}
