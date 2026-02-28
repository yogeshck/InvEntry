using CommunityToolkit.Mvvm.ComponentModel;
using InvEntry.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InvEntry.Models
{
    public partial class StockVerifyScan : BaseEntity
    {

        [ObservableProperty]
        public long? _sessionId;

        [ObservableProperty]
        public string? _barcode;

        [ObservableProperty]
        public DateTime _scanTime;

        [ObservableProperty]
        public string? _status;
    }
}
