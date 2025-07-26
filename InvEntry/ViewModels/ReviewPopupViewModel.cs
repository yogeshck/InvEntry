using CommunityToolkit.Mvvm.ComponentModel;
using InvEntry.Models;

namespace InvEntry.ViewModels
{

    public partial class ReviewPopupViewModel : ObservableObject
    {
        public EstimateHeader Estimate { get; set; }
        public bool Confirmed { get; set; }

        public ReviewPopupViewModel(EstimateHeader estimate)
        {
            Estimate = estimate;
        }
        
    }

    
}
