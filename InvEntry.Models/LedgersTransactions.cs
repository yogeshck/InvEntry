using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InvEntry.Models
{
    public partial class LedgersTransactions : BaseEntity
    {

        [ObservableProperty]
        public int _ledgerHdrGkey;

        [ObservableProperty]
        public string? _drCr;

        [ObservableProperty]
        public DateTime? _transactionDate;

        [ObservableProperty]
        public decimal? _transactionAmount;

        [ObservableProperty]
        public bool? _status;

        [ObservableProperty]
        public string? _documentNbr;

        [ObservableProperty]
        public DateTime? _documentDate;

        [NotMapped]
        [ObservableProperty]
        private string? _transType;

    }
}
