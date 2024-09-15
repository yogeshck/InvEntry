using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace InvEntry.Models
{
    public partial class DailyMetalRate : BaseEntity
    {
        [ObservableProperty]
        private string metalName;

        [ObservableProperty]
        private string purity;

        [ObservableProperty]
        private DateTime? effectiveDate;

        [ObservableProperty]
        private string carat;

        [ObservableProperty]
        private decimal? price;

        [property: JsonIgnore]
        public MetalType metalType 
        {
            get 
            {
                if(Enum.TryParse<MetalType>(MetalName, out var value))
                    return value;
                return MetalType.Gold;
            }
        }
    }
}
