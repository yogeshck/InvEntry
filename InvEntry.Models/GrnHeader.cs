using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using InvEntry.Models;

namespace InvEntry.Models;

public partial class GrnHeader : BaseEntity
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
        public string? _customerOrderNbr;

        [ObservableProperty]
        public string? _documentRef;

        [ObservableProperty]
        public DateTime? _documentDate;

    
}
