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
        public int? _refGkey;

        [ObservableProperty]
        public DateTime? _transactionDate;

        [ObservableProperty]
        public string? _productCategory;

        [ObservableProperty]
        public string? _productSku;

        [ObservableProperty]
        public string? _transactionType;

        [ObservableProperty]
        public string? _documentNbr;

        [ObservableProperty]
        public string? _documentType;

        [ObservableProperty]
        public string? _voucherType;

        [ObservableProperty]
        public int? _obQty;

        [ObservableProperty]
        public int? _transactionQty;

        [ObservableProperty]
        public int? _cbQty;

        [ObservableProperty]
        public decimal? _obWeight;

        [ObservableProperty]
        public decimal? _transactionWeight;

        [ObservableProperty]
        public decimal? _cbWeight;

        [ObservableProperty]
        public decimal? _unitPrice;

        [ObservableProperty]
        public decimal? _transactionValue;

        [ObservableProperty]
        public string? _notes;

    }
}
