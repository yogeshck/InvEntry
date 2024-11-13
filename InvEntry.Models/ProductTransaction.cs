using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InvEntry.Models
{
    public partial class ProductTransaction : BaseEntity
    {
        [ObservableProperty]
        public double? _refGkey;

        [ObservableProperty]
        public DateTime? _transDate;

        [ObservableProperty]
        public string? _docRefNbr;

        [ObservableProperty]
        public string? _docType;

        [ObservableProperty]
        public int _productRefGkey;

        [ObservableProperty]
        public decimal? _column1;

        [ObservableProperty]
        public decimal? _transQty;

        [ObservableProperty]
        public decimal? _cbQty;

        [ObservableProperty]
        public decimal? _unitTransPrice;

        [ObservableProperty]
        public decimal? _transactionValue;

        [ObservableProperty]
        public string? _transNote;
    }
}
