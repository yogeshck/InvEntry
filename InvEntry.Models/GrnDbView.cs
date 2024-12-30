using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InvEntry.Models
{
    public partial class GrnDbView : BaseEntity
    {

        [ObservableProperty]
        public string? _grnNbr;

        [ObservableProperty]
        public DateTime? _grnDate;

        [ObservableProperty]
        public string? _documentType;

        [ObservableProperty]
        public string? _orderNbr;

        [ObservableProperty]
        public DateTime? _orderDate;

        [ObservableProperty]
        public string? _supplierId;

        [ObservableProperty]
        public string? _supplierRefNbr;

        [ObservableProperty]
        public DateTime? _itemReceivedDate;

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
