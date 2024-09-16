using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace InvEntry.Models
{
    public partial class DailyRate : BaseEntity
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
    }
}
