using CommunityToolkit.Mvvm.ComponentModel;
using InvEntry.Models;
using System.Collections.ObjectModel;
using System.Text.Json.Serialization;

namespace InvEntry.Models
{

    public partial class LedgersHeader : BaseEntity
    {
        public LedgersHeader()
        {
            Transactions = new();
        }

        [ObservableProperty]
        public int? _mtblLedgersGkey;

        [ObservableProperty]
        public int? _custGkey;

        [ObservableProperty]
        public DateTime? _balanceAsOn;

        [ObservableProperty]
        public decimal? _currentBalance;

        [ObservableProperty]
        [property: JsonIgnore]
        private ObservableCollection<LedgersTransactions>? transactions;
    }
}
