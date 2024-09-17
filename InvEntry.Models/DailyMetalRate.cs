using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace InvEntry.Models
{
    public partial class DailyRate : BaseEntity, IEqualityComparer<DailyRate>
    {
        [ObservableProperty]
        private string? metal;

        [ObservableProperty]
        private string? purity;

        [ObservableProperty]
        private DateTime effectiveDate;

        [ObservableProperty]
        private string? carat;

        [ObservableProperty]
        private decimal? price;

        [ObservableProperty]
        private bool isDisplay;

        [property: JsonIgnore]
        public MetalType MetalType 
        {
            get 
            {
                if(Enum.TryParse<MetalType>(Metal, out var value))
                    return value;
                return MetalType.Gold;
            }
        }

        public bool Equals(DailyRate? x, DailyRate? y)
        {
            return x is not null
                && y is not null
                && x.Metal.Equals(y.Metal, StringComparison.OrdinalIgnoreCase)
                && x.Carat.Equals(y.Carat, StringComparison.OrdinalIgnoreCase)
                && x.Purity.Equals(y.Purity, StringComparison.OrdinalIgnoreCase);
        }

        public int GetHashCode([DisallowNull] DailyRate obj)
        {
            return obj.GetHashCode();
        }
    }
}
